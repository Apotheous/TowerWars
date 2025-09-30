using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


public class SoldiersControllerNavMesh : NetworkBehaviour
{
    [SerializeField] private NavMeshAgent navMesh;
    [SerializeField] private Transform target;



    public IEnumerator FindTargetAndSetDestination()
    {
        var myIdentity = GetComponent<UnitIdentity>();
        if (myIdentity == null)
        {
            Debug.LogError("[SERVER] UnitIdentity script'i bulunamad�!", gameObject);
            yield break;
        }

        Debug.Log($"[SERVER] Birim spawn oldu. Gelen TeamID: {myIdentity.TeamId.Value}. Hedef aran�yor...");

        int attempts = 0;
        while (target == null && attempts < 10)
        {
            attempts++;
            if (myIdentity.TeamId.Value == 1)
            {
                Debug.Log($"[SERVER] TeamID "+myIdentity.TeamId.Value+", Player1 hedefi aran�yor... ");
                target = DevSingletonTransform.instance.player2Transform;
            }
            else if (myIdentity.TeamId.Value == 2)
            {
                Debug.Log($"[SERVER] TeamID " + myIdentity.TeamId.Value + ", Player1 hedefi aran�yor... ");
                target = DevSingletonTransform.instance.player1Transform;
            }

            if (target == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Hedef atamas� yap�ld�ktan sonra...
        if (target != null)
        {
            Debug.Log($"[SERVER] HEDEF ATAMASI BA�ARILI: {target.name}. �imdi NavMesh kontrolleri yap�l�yor...");

            // --- NAVMESH KONTROL ADIMLARI ---

            // 1. Kontrol: Bu birimin kendisi NavMesh �zerinde mi?
            bool isAgentOnNavMesh = navMesh.isOnNavMesh;
            Debug.Log($"[SERVER CHECK] Bu birim NavMesh �zerinde mi? -> {isAgentOnNavMesh}");

            // 2. Kontrol: Hedefin pozisyonu NavMesh �zerinde mi? (1 metrelik bir toleransla)
            NavMeshHit hit;
            bool isTargetOnNavMesh = NavMesh.SamplePosition(target.position, out hit, 1.0f, NavMesh.AllAreas);
            Debug.Log($"[SERVER CHECK] Hedef NavMesh �zerinde mi? -> {isTargetOnNavMesh}");

            // 3. Kontrol (En �nemlisi): Bu birimden hedefe ge�erli bir yol var m�?
            NavMeshPath path = new NavMeshPath();
            bool pathCalculated = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
            if (pathCalculated)
            {
                Debug.Log($"[SERVER CHECK] Hedefe giden bir yol var m�? -> DURUM: {path.status}");
            }
            else
            {
                Debug.LogError($"[SERVER CHECK] Hedefe giden bir yol HESAPLANAMADI! Ba�lang�� veya biti� noktas� �ok uzakta olabilir.");
            }

            // --- KONTROLLER B�TT� ---

            // T�m kontroller ba�ar�l�ysa hareket komutunu ver
            if (isAgentOnNavMesh && isTargetOnNavMesh && path.status == NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"[SERVER] T�m NavMesh kontrolleri ba�ar�l�. Harekete ge�iliyor!");
                navMesh.destination = target.position;
            }
            else
            {
                Debug.LogError($"[SERVER] NavMesh kontrolleri ba�ar�s�z oldu�u i�in hareket ETT�R�LMED�. L�tfen yukar�daki [SERVER CHECK] loglar�n� inceleyin.");
            }
        }
        else
        {
            Debug.LogError("[SERVER] 10 denemeye ra�men hedef ATANAMADI!");
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (target != null)
        {
            if (navMesh.destination != target.position)
            {
                navMesh.SetDestination(target.position);
            }
        }
        // E�er bir sebepten �t�r� hedef yok olduysa ve biz hala hedefsizsek, tekrar aramay� dene.
        else if (navMesh.hasPath == false)
        {
            // Bu k�sm� projenin mant���na g�re daha sonra doldurabilirsin.
            // �rne�in, en yak�n d��man� bul gibi.
        }
    }
}
