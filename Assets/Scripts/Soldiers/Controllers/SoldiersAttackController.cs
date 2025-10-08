using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Soldier))] // Bu script'in çalýþmasý için bir Soldier script'i gerektiðini belirtir.
[RequireComponent(typeof(SoldiersControllerNavMesh))]
public class SoldiersAttackController : NetworkBehaviour
{
    [Header("Saldýrý Ayarlarý")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireRate = 1f; // Saniyede kaç atýþ
    [SerializeField] private float damage = 10f;

    [Header("Mermi Ayarlarý")]
    [SerializeField] private GameObject bulletPrefab; // Inspector'dan mermi prefab'ýný sürükle
    [SerializeField] private Transform firePoint; // Merminin çýkacaðý yer (namlu ucu vb.)

    // Diðer component'lere referanslar
    private SoldiersControllerNavMesh movementController;
    private Soldier mySoldierInfo;


    private Coroutine attackCoroutine; // Çalýþan Coroutine'e referans tutar.

    // Gecikme hesaplamasý için önceden tanýmlanmýþ bir WaitForSeconds objesi
    private WaitForSeconds attackDelay ;

    private void Awake()
    {
        movementController = GetComponent<SoldiersControllerNavMesh>();
        mySoldierInfo = GetComponent<Soldier>(); // Kendi Soldier script'imizi alýyoruz.
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            this.enabled = false;
            return;
        }
        // Yalnýzca Sunucuda: fireRate deðerine göre attackDelay'i hesapla ve ayarla.
        if (fireRate > 0)
        {
            // Örn: fireRate 2 ise (saniyede 2 atýþ), bekleme süresi 1 / 2 = 0.5 saniye olmalý.
            float delayTime = 1f / fireRate;
            attackDelay = new WaitForSeconds(delayTime);
            Debug.Log($"[SERVER ATTACK] Saldýrý Gecikmesi Ayarlandý: {delayTime} saniye.");
        }
        else
        {
            // fireRate 0 veya negatif ise, hata oluþmamasý için varsayýlan bir deðer verilir.
            attackDelay = new WaitForSeconds(0.1f);
            Debug.LogError("FireRate sýfýr veya negatif! Varsayýlan gecikme (0.1s) kullanýlýyor.");
        }
    }

    /// <summary>
    /// Saldýrý döngüsünü baþlatýr. (Örn: Hedef menzile girdiðinde SoldiersControllerNavMesh çaðýrabilir)
    /// </summary>
    public void StartAttacking(Transform currentTarget)
    {
        // Zaten bir saldýrý döngüsü çalýþýyorsa tekrar baþlatma.
        if (attackCoroutine != null) return;

        // Saldýrý Coroutine'ini baþlat.
        attackCoroutine = StartCoroutine(AttackCoroutine(currentTarget));
    }

    /// <summary>
    /// Saldýrý döngüsünü durdurur. (Örn: Hedef yok olduðunda/menzilden çýktýðýnda çaðrýlabilir)
    /// </summary>
    public void StopAttacking()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null; // Coroutine'i durdurduktan sonra referansý temizle.
        }
    }

    /// <summary>
    /// Coroutine: Belirlenen atýþ hýzýna göre sürekli ateþ eder.
    /// </summary>
    private IEnumerator AttackCoroutine(Transform currentTarget)
    {
        while (true) // Coroutine'i StopAttacking() çaðrýlana kadar sonsuza kadar çalýþtýr.
        {
            

            // 1. Hedef varlýðýný ve menzili kontrol et.
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

                if (distanceToTarget <= attackRange)
                {
                    // Menzildeyse: Yönünü hedefe çevir ve ateþ et.
                    transform.LookAt(currentTarget.position);
                    Attack();
                }
                // Hedeften çýkarsa, bu Coroutine devam eder ama ateþ etmez.
                // Ýsteðe baðlý olarak, menzil dýþýndaysa StopAttacking() çaðrýlabilir.
            }

            // 2. Belirlenen atýþ hýzý kadar bekle.
            yield return attackDelay;
        }
    }

    /// <summary>
    /// Mermi oluþturma ve network'te spawn etme iþlemini yapar.
    /// </summary>
    private void Attack()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab veya Fire Point atanmamýþ!");
            // Hata varsa Coroutine'i durdurmak mantýklý olabilir.
            StopAttacking();
            return;
        }

        // 1. Mermiyi server'da oluþtur.
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 2. Merminin script'ini al ve GÖREVÝNÝ VER (takým kimliði ve hasar).
        // NOT: TestBullet sýnýfý ve Initialize metodu varsayýlmýþtýr.
        TestBullet bulletScript = bulletInstance.GetComponent<TestBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(mySoldierInfo.TeamId.Value, damage);
        }
        else
        {
            Debug.LogError("Mermi Prefab'ýnda TestBullet script'i bulunamadý!");
        }

        // 3. Mermiyi network'te spawn et ki client'lar da görsün.
        NetworkObject netObj = bulletInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }
        else
        {
            Debug.LogError("Mermi Prefab'ýnda NetworkObject bulunamadý!");
        }
    }

}
