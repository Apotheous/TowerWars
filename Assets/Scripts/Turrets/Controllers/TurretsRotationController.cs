using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class TurretsRotationController : NetworkBehaviour
{
    [SerializeField] Transform returnX, returnY;
    // Inspector'dan atanmas� gereken NavMeshAgent bile�eni
    private NavMeshAgent navMesh;

    // Askerin mevcut hedefini tutar
    private Transform currentEnemyTarget; // �simlendirme netle�tirildi
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


        // 1. D��MAN HEDEF� �NCEL���: D��man varsa ona git
        if (currentEnemyTarget != null)
        {
            // E�er durdurulmu�sa (StopUnit ile), yeniden ba�lat
            if (navMesh.isStopped)
            {
                navMesh.isStopped = false;
                // Sald�r� Controller'� zaten h�z� ayarlayacakt�r, ama emin olmak i�in:
                // navMesh.speed = soldier.MovementSpeed;
            }

            navMesh.SetDestination(currentEnemyTarget.position);
        }
        else if (baseTarget != null)
        {
            // E�er durdurulmu�sa (StopUnit ile), yeniden ba�lat
            if (navMesh.isStopped)
            {
                navMesh.isStopped = false;
                // navMesh.speed = soldier.MovementSpeed;
            }

            navMesh.SetDestination(baseTarget.position);
        }
        else
        {
            // Bu durum, genellikle sadece baseTarget atanmad���nda (Spawner hatas�)
            // veya GiveMeNewTarget(null) sonras� hareketin durmas� gerekti�inde tetiklenmeli.
            if (navMesh.hasPath)
            {
                navMesh.ResetPath();
                navMesh.isStopped = true;
                // navMesh.velocity = Vector3.zero; // ResetPath ve isStopped=true ile genellikle gerek kalmaz
                Debug.Log($"[SERVER/NavMesh] {gameObject.name} i�in hedef yok, hareket durduruldu.");
            }
        }
    }
    /// <summary>
    /// Bir d��man hedefini (TargetDetector'dan gelen) ayarlar.
    /// E�er null gelirse, birimin ana hedefine (baseTarget) geri d�nmesini sa�lar.
    /// </summary>

    public void GiveMeNewTarget(Transform newTarget)
    {
        if (!IsServer || navMesh == null || !navMesh.isOnNavMesh) return;


        currentEnemyTarget = newTarget;

        if (currentEnemyTarget != null)
        {
            navMesh.isStopped = false; // Hareket etmeye ba�la
            navMesh.SetDestination(currentEnemyTarget.position);
            Debug.Log($"[SERVER/NavMesh] Yeni D��MAN hedefi al�nd�: ({currentEnemyTarget.name}).");
        }
        else // D��man yoksa, baseTarget'a d�nme mant��� Update'e b�rak�lm��t�r.
        {
            // E�er currentEnemyTarget null ise, Update d�ng�s� otomatik olarak baseTarget'� hedefler.
            // Burada sadece loglay�p ��kmak yeterli.
            Debug.Log("[SERVER/NavMesh] D��man hedefi temizlendi. Birim varsay�lan hedefine d�necek.");
        }
    }

    // Harici birimlerin mevcut hedefi almas�n� sa�lar (Opsiyonel)
    public Transform GetCurrentTarget()
    {
        return currentEnemyTarget;
    }
    // Harici birimlerin mevcut base hedefi almas�n� sa�lar (Opsiyonel)
    public Transform GetCurrentBaseTarget()
    {
        return baseTarget;
    }

    public void StopUnit()
    {
        if (IsServer && navMesh != null)
        {
            // En g�venilir durdurma metodu budur.
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

� � � � // Hedef atamas� yap�ld�ktan sonra...
� � � � if (baseTarget != null)
        {
            GiveMeNewTarget(baseTarget);
        }
    }
}
