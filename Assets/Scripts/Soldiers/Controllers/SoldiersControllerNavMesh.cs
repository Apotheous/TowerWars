using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


public class SoldiersControllerNavMesh : NetworkBehaviour
{
    // Inspector'dan atanması gereken NavMeshAgent bileşeni
    [SerializeField] private NavMeshAgent navMesh;

    // Askerin mevcut hedefini tutar
    [SerializeField]private Transform currentTarget;
    [SerializeField]private Transform baseTarget;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            this.enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (navMesh == null)
        {
            Debug.LogError($"[SERVER/NavMesh] {gameObject.name} için NavMeshAgent atanmadı!");
            return;
        }
        if (currentTarget != null)
        {
            // Hedefe doğru yönlendir
            if (navMesh.isOnNavMesh)
            {
                navMesh.SetDestination(currentTarget.position);
            }
            else
            {
                Debug.LogError($"[SERVER/NavMesh] {gameObject.name} NavMesh üzerinde değil. Hedefe yönlendirilemiyor.");
            }
        }
        else if (baseTarget!=null)
        {
            // Hedef yoksa dur
            // Hedefe doğru yönlendir
            if (navMesh.isOnNavMesh)
            {
                navMesh.SetDestination(baseTarget.position);
            }
        }
        else
        {
            // Hedef yoksa dur
            if (navMesh.hasPath)
            {
                navMesh.ResetPath();
                Debug.Log($"[SERVER/NavMesh] {gameObject.name} için hedef null, hareket durduruldu.");
            }
        }
    }
    public void GiveMeNewTarget(Transform newTarget)
    {

        if (newTarget == null)
        {
            Debug.LogWarning("[SERVER/NavMesh] Yeni hedef null. Atama yapılmadı.");
            return;
        }

        currentTarget = newTarget;

        if (navMesh != null && navMesh.isOnNavMesh)
        {
            navMesh.ResetPath(); // eski path'i temizle
            navMesh.SetDestination(currentTarget.position); // yeni hedefe yönlendir
            Debug.Log($"[SERVER/NavMesh] Yeni hedef alındı: ({currentTarget.name}). Yönlendirme başlatıldı.");
        }
        else
        {
            Debug.LogError($"[SERVER/NavMesh] {gameObject.name} NavMesh üzerinde değil veya Agent bileşeni yok. Hareket ettirilemiyor.");
        }
    }

    // Harici birimlerin mevcut hedefi almasını sağlar (Opsiyonel)
    public Transform GetCurrentTarget()
    {        
        return currentTarget;
    }

    public void StopUnit()
    {
        if (IsServer)
        {
            // NavMeshAgent'ın var olup olmadığını kontrol etmek iyi bir pratik.
            if (navMesh != null)
            {
                // 1. **isStopped = true** yapmak, Agent'ın mevcut hedefine gitmeyi hemen bırakmasını sağlar.
                // Bu, durdurma için en temel ve önerilen yöntemdir.
                navMesh.isStopped = true;

                // 2. **speed = 0f** yapmak, yeni bir hedef atandığında bile Agent'ın hareket etmemesini garanti eder.
                // Ancak, isStopped = true ise bu genellikle gereksizdir. Yine de emin olmak için kullanılabilir.
                navMesh.speed = 0f;

                // 3. **velocity = Vector3.zero** yapmak, anlık hızı sıfırlar. 
                // navMesh.isStopped = true yapıldığında motor bunu zaten halleder, 
                // ancak ani duruş sağlamak için bazen eklenir. Genellikle sadece isStopped yeterlidir.
                navMesh.velocity = Vector3.zero;

                // ✨ NOT: Genellikle sadece 'navMesh.isStopped = true;' kullanmak yeterlidir.
                Debug.Log($"[SERVER/NavMesh] {gameObject.name} birimi durduruldu. "+"NavmeshSpeed =="+navMesh.speed + "NavmeshSpeedtoString ==" + navMesh.speed.ToString());
            }
        }
    }
 
    public IEnumerator FindTargetAndSetDestination()
    {

        var myIdentity = GetComponent<Soldier>();

        if (myIdentity == null)
        {
            yield break;
        }

        int attempts = 0;

        while (baseTarget == null && attempts < 10)
        {

            attempts++;

            if (myIdentity.TeamId.Value == 1)

            {
                baseTarget = DevSingletonTransform.instance.player2Transform;
            }

            else if (myIdentity.TeamId.Value == 2)
            {
                baseTarget = DevSingletonTransform.instance.player1Transform;
            }
            if (baseTarget == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

        }

        // Hedef ataması yapıldıktan sonra...
        if (baseTarget != null)
        {
            GiveMeNewTarget(baseTarget);
        }
    }
}
