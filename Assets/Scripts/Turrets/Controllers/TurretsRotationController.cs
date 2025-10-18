using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class TurretsRotationController : NetworkBehaviour
{
    [SerializeField] Transform returnX, returnY;
    // Inspector'dan atanmasý gereken NavMeshAgent bileþeni
    private NavMeshAgent navMesh;

    // Askerin mevcut hedefini tutar
    private Transform currentEnemyTarget; // Ýsimlendirme netleþtirildi
    private Transform baseTarget;

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
        if (!IsServer || navMesh == null || !navMesh.isOnNavMesh) return;


        // 1. DÜÞMAN HEDEFÝ ÖNCELÝÐÝ: Düþman varsa ona git
        if (currentEnemyTarget != null)
        {
            // Eðer durdurulmuþsa (StopUnit ile), yeniden baþlat
            if (navMesh.isStopped)
            {
                navMesh.isStopped = false;
                // Saldýrý Controller'ý zaten hýzý ayarlayacaktýr, ama emin olmak için:
                // navMesh.speed = soldier.MovementSpeed;
            }

            navMesh.SetDestination(currentEnemyTarget.position);
        }
        else if (baseTarget != null)
        {
            // Eðer durdurulmuþsa (StopUnit ile), yeniden baþlat
            if (navMesh.isStopped)
            {
                navMesh.isStopped = false;
                // navMesh.speed = soldier.MovementSpeed;
            }

            navMesh.SetDestination(baseTarget.position);
        }
        else
        {
            // Bu durum, genellikle sadece baseTarget atanmadýðýnda (Spawner hatasý)
            // veya GiveMeNewTarget(null) sonrasý hareketin durmasý gerektiðinde tetiklenmeli.
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
    /// Bir düþman hedefini (TargetDetector'dan gelen) ayarlar.
    /// Eðer null gelirse, birimin ana hedefine (baseTarget) geri dönmesini saðlar.
    /// </summary>

    public void GiveMeNewTarget(Transform newTarget)
    {
        if (!IsServer || navMesh == null || !navMesh.isOnNavMesh) return;


        currentEnemyTarget = newTarget;

        if (currentEnemyTarget != null)
        {
            navMesh.isStopped = false; // Hareket etmeye baþla
            navMesh.SetDestination(currentEnemyTarget.position);
            Debug.Log($"[SERVER/NavMesh] Yeni DÜÞMAN hedefi alýndý: ({currentEnemyTarget.name}).");
        }
        else // Düþman yoksa, baseTarget'a dönme mantýðý Update'e býrakýlmýþtýr.
        {
            // Eðer currentEnemyTarget null ise, Update döngüsü otomatik olarak baseTarget'ý hedefler.
            // Burada sadece loglayýp çýkmak yeterli.
            Debug.Log("[SERVER/NavMesh] Düþman hedefi temizlendi. Birim varsayýlan hedefine dönecek.");
        }
    }

    // Harici birimlerin mevcut hedefi almasýný saðlar (Opsiyonel)
    public Transform GetCurrentTarget()
    {
        return currentEnemyTarget;
    }
    // Harici birimlerin mevcut base hedefi almasýný saðlar (Opsiyonel)
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

        // Hedef atamasý yapýldýktan sonra...
        if (baseTarget != null)
        {
            GiveMeNewTarget(baseTarget);
        }
    }
}
