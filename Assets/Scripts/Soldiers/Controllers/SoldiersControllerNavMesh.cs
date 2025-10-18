using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


public class SoldiersControllerNavMesh : NetworkBehaviour
{
    // Inspector'dan atanması gereken NavMeshAgent bileşeni
    private NavMeshAgent navMesh;

    // Askerin mevcut hedefini tutar
    private Transform currentEnemyTarget; // İsimlendirme netleştirildi
    private Transform baseTarget;

    public override void OnNetworkSpawn()
    {
        navMesh = GetComponent<NavMeshAgent>();
        if (!IsServer)
        {
            this.enabled = false;
            
            return;
        }
    }

    private void Update()
    {
        if (!IsServer || navMesh == null || !navMesh.isOnNavMesh) return;


        // 1. DÜŞMAN HEDEFİ ÖNCELİĞİ: Düşman varsa ona git
        if (currentEnemyTarget != null)
        {
            // Eğer durdurulmuşsa (StopUnit ile), yeniden başlat
            if (navMesh.isStopped)
            {
                navMesh.isStopped = false;
                // Saldırı Controller'ı zaten hızı ayarlayacaktır, ama emin olmak için:
                // navMesh.speed = soldier.MovementSpeed;
            }

            navMesh.SetDestination(currentEnemyTarget.position);
        }
        else if (baseTarget!=null)
        {
            // Eğer durdurulmuşsa (StopUnit ile), yeniden başlat
            if (navMesh.isStopped)
            {
                navMesh.isStopped = false;
                // navMesh.speed = soldier.MovementSpeed;
            }

            navMesh.SetDestination(baseTarget.position);
        }
        else
        {
            // Bu durum, genellikle sadece baseTarget atanmadığında (Spawner hatası)
            // veya GiveMeNewTarget(null) sonrası hareketin durması gerektiğinde tetiklenmeli.
            if (navMesh.hasPath)
            {
                navMesh.ResetPath();
                navMesh.isStopped = true;
                // navMesh.velocity = Vector3.zero; // ResetPath ve isStopped=true ile genellikle gerek kalmaz
                Debug.Log($"[SERVER/NavMesh] {gameObject.name} için hedef yok, hareket durduruldu.");
            }
        }
    }
    /// <summary>
    /// Bir düşman hedefini (TargetDetector'dan gelen) ayarlar.
    /// Eğer null gelirse, birimin ana hedefine (baseTarget) geri dönmesini sağlar.
    /// </summary>

    public void GiveMeNewTarget(Transform newTarget)
    {
        if (!IsServer || navMesh == null || !navMesh.isOnNavMesh) return;
    

        currentEnemyTarget = newTarget;

        if (currentEnemyTarget != null)
        {
            navMesh.isStopped = false; // Hareket etmeye başla
            navMesh.SetDestination(currentEnemyTarget.position);
            Debug.Log($"[SERVER/NavMesh] Yeni DÜŞMAN hedefi alındı: ({currentEnemyTarget.name}).");
        }
        else // Düşman yoksa, baseTarget'a dönme mantığı Update'e bırakılmıştır.
        {
            // Eğer currentEnemyTarget null ise, Update döngüsü otomatik olarak baseTarget'ı hedefler.
            // Burada sadece loglayıp çıkmak yeterli.
            Debug.Log("[SERVER/NavMesh] Düşman hedefi temizlendi. Birim varsayılan hedefine dönecek.");
        }
    }

    // Harici birimlerin mevcut hedefi almasını sağlar (Opsiyonel)
    public Transform GetCurrentTarget()
    {        
        return currentEnemyTarget;
    }
    // Harici birimlerin mevcut base hedefi almasını sağlar (Opsiyonel)
    public Transform GetCurrentBaseTarget()
    {        
        return baseTarget;
    }

    public void StopUnit()
    {
        if (IsServer && navMesh != null)
        {
            // En güvenilir durdurma metodu budur.
            navMesh.isStopped = true;
            navMesh.velocity = Vector3.zero;
            Debug.Log($"[SERVER/NavMesh] {gameObject.name} birimi zorla durduruldu.");
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
