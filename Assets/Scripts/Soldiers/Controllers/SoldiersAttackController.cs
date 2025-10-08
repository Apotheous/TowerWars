using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Soldier))] // Bu script'in �al��mas� i�in bir Soldier script'i gerekti�ini belirtir.
[RequireComponent(typeof(SoldiersControllerNavMesh))]
public class SoldiersAttackController : NetworkBehaviour
{
    [Header("Sald�r� Ayarlar�")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireRate = 1f; // Saniyede ka� at��
    [SerializeField] private float damage = 10f;

    [Header("Mermi Ayarlar�")]
    [SerializeField] private GameObject bulletPrefab; // Inspector'dan mermi prefab'�n� s�r�kle
    [SerializeField] private Transform firePoint; // Merminin ��kaca�� yer (namlu ucu vb.)

    // Di�er component'lere referanslar
    private SoldiersControllerNavMesh movementController;
    private Soldier mySoldierInfo;


    private Coroutine attackCoroutine; // �al��an Coroutine'e referans tutar.

    // Gecikme hesaplamas� i�in �nceden tan�mlanm�� bir WaitForSeconds objesi
    private WaitForSeconds attackDelay ;

    private void Awake()
    {
        movementController = GetComponent<SoldiersControllerNavMesh>();
        mySoldierInfo = GetComponent<Soldier>(); // Kendi Soldier script'imizi al�yoruz.
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            this.enabled = false;
            return;
        }
        // Yaln�zca Sunucuda: fireRate de�erine g�re attackDelay'i hesapla ve ayarla.
        if (fireRate > 0)
        {
            // �rn: fireRate 2 ise (saniyede 2 at��), bekleme s�resi 1 / 2 = 0.5 saniye olmal�.
            float delayTime = 1f / fireRate;
            attackDelay = new WaitForSeconds(delayTime);
            Debug.Log($"[SERVER ATTACK] Sald�r� Gecikmesi Ayarland�: {delayTime} saniye.");
        }
        else
        {
            // fireRate 0 veya negatif ise, hata olu�mamas� i�in varsay�lan bir de�er verilir.
            attackDelay = new WaitForSeconds(0.1f);
            Debug.LogError("FireRate s�f�r veya negatif! Varsay�lan gecikme (0.1s) kullan�l�yor.");
        }
    }

    /// <summary>
    /// Sald�r� d�ng�s�n� ba�lat�r. (�rn: Hedef menzile girdi�inde SoldiersControllerNavMesh �a��rabilir)
    /// </summary>
    public void StartAttacking(Transform currentTarget)
    {
        // Zaten bir sald�r� d�ng�s� �al���yorsa tekrar ba�latma.
        if (attackCoroutine != null) return;

        // Sald�r� Coroutine'ini ba�lat.
        attackCoroutine = StartCoroutine(AttackCoroutine(currentTarget));
    }

    /// <summary>
    /// Sald�r� d�ng�s�n� durdurur. (�rn: Hedef yok oldu�unda/menzilden ��kt���nda �a�r�labilir)
    /// </summary>
    public void StopAttacking()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null; // Coroutine'i durdurduktan sonra referans� temizle.
        }
    }

    /// <summary>
    /// Coroutine: Belirlenen at�� h�z�na g�re s�rekli ate� eder.
    /// </summary>
    private IEnumerator AttackCoroutine(Transform currentTarget)
    {
        while (true) // Coroutine'i StopAttacking() �a�r�lana kadar sonsuza kadar �al��t�r.
        {
            

            // 1. Hedef varl���n� ve menzili kontrol et.
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

                if (distanceToTarget <= attackRange)
                {
                    // Menzildeyse: Y�n�n� hedefe �evir ve ate� et.
                    transform.LookAt(currentTarget.position);
                    Attack();
                }
                // Hedeften ��karsa, bu Coroutine devam eder ama ate� etmez.
                // �ste�e ba�l� olarak, menzil d���ndaysa StopAttacking() �a�r�labilir.
            }

            // 2. Belirlenen at�� h�z� kadar bekle.
            yield return attackDelay;
        }
    }

    /// <summary>
    /// Mermi olu�turma ve network'te spawn etme i�lemini yapar.
    /// </summary>
    private void Attack()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab veya Fire Point atanmam��!");
            // Hata varsa Coroutine'i durdurmak mant�kl� olabilir.
            StopAttacking();
            return;
        }

        // 1. Mermiyi server'da olu�tur.
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 2. Merminin script'ini al ve G�REV�N� VER (tak�m kimli�i ve hasar).
        // NOT: TestBullet s�n�f� ve Initialize metodu varsay�lm��t�r.
        TestBullet bulletScript = bulletInstance.GetComponent<TestBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(mySoldierInfo.TeamId.Value, damage);
        }
        else
        {
            Debug.LogError("Mermi Prefab'�nda TestBullet script'i bulunamad�!");
        }

        // 3. Mermiyi network'te spawn et ki client'lar da g�rs�n.
        NetworkObject netObj = bulletInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }
        else
        {
            Debug.LogError("Mermi Prefab'�nda NetworkObject bulunamad�!");
        }
    }

}
