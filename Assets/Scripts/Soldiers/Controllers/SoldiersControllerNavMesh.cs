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
        var myIdentity = GetComponent<Soldier>();
        if (myIdentity == null)
        {
            
            yield break;
        }


        int attempts = 0;
        while (target == null && attempts < 10)
        {
            attempts++;
            if (myIdentity.TeamId.Value == 1)
            {

                target = DevSingletonTransform.instance.player2Transform;
            }
            else if (myIdentity.TeamId.Value == 2)
            {

                target = DevSingletonTransform.instance.player1Transform;
            }

            if (target == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Hedef atamasý yapýldýktan sonra...
        if (target != null)
        {


            // --- NAVMESH KONTROL ADIMLARI ---

            // 1. Kontrol: Bu birimin kendisi NavMesh üzerinde mi?
            bool isAgentOnNavMesh = navMesh.isOnNavMesh;


            // 2. Kontrol: Hedefin pozisyonu NavMesh üzerinde mi? (1 metrelik bir toleransla)
            NavMeshHit hit;
            bool isTargetOnNavMesh = NavMesh.SamplePosition(target.position, out hit, 1.0f, NavMesh.AllAreas);


            // 3. Kontrol (En Önemlisi): Bu birimden hedefe geçerli bir yol var mý?
            NavMeshPath path = new NavMeshPath();
            bool pathCalculated = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
            if (pathCalculated)
            {
                Debug.Log($"[SERVER CHECK] Hedefe giden bir yol var mý? -> DURUM: {path.status}");
            }
            else
            {
                Debug.LogError($"[SERVER CHECK] Hedefe giden bir yol HESAPLANAMADI! Baþlangýç veya bitiþ noktasý çok uzakta olabilir.");
            }

            // --- KONTROLLER BÝTTÝ ---

            // Tüm kontroller baþarýlýysa hareket komutunu ver
            if (isAgentOnNavMesh && isTargetOnNavMesh && path.status == NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"[SERVER] Tüm NavMesh kontrolleri baþarýlý. Harekete geçiliyor!");
                navMesh.destination = target.position;
            }
            else
            {
                Debug.LogError($"[SERVER] NavMesh kontrolleri baþarýsýz olduðu için hareket ETTÝRÝLMEDÝ. Lütfen yukarýdaki [SERVER CHECK] loglarýný inceleyin.");
            }
        }
        else
        {
            Debug.LogError("[SERVER] 10 denemeye raðmen hedef ATANAMADI!");
        }
    }

    // SoldiersControllerNavMesh.cs içinde
    public Transform GetCurrentTarget()
    {
        return target;
    }
    public void GiveMeNewTarget(Transform newTarget)
    {
        target = newTarget;
        if (IsServer && target != null)
        {
            navMesh.SetDestination(target.position);
            Debug.Log($"[NavMesh] Düþman birim alýndý ({target.name}) hedefine yoneldi.");
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
        // Eðer bir sebepten ötürü hedef yok olduysa ve biz hala hedefsizsek, tekrar aramayý dene.
        else if (navMesh.hasPath == false)
        {
            // Bu kýsmý projenin mantýðýna göre daha sonra doldurabilirsin.
            // Örneðin, en yakýn düþmaný bul gibi.
        }
    }
}
