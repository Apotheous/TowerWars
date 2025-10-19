using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



[RequireComponent(typeof(Turret))] // Bu script'in çalýþmasý için bir Soldier script'i gerektiðini belirtir.
[RequireComponent(typeof(TurretsRotationController))]
public class TurretsAttackController : NetworkBehaviour
{
    [Header("Saldýrý Ayarlarý")]
    private float attackRange = 9999f;
    [SerializeField] private float fireRate = 1f; // Saniyede kaç atýþ
    [SerializeField] private float damage = 10f;

    [Header("Mermi Ayarlarý")]
    [SerializeField] private GameObject bulletPrefab; // Inspector'dan mermi prefab'ýný sürükle
    [SerializeField] private Transform firePoint; // Merminin çýkacaðý yer (namlu ucu vb.)


    private Turret mySoldierInfo;//Burada Kaldým
    private TargetDetector myTargetDetector; // YENÝ: TargetDetector referansý
    private Transform myCurrentTarget;

    private Coroutine attackCoroutine; // Çalýþan Coroutine'e referans tutar.

    // Gecikme hesaplamasý için önceden tanýmlanmýþ bir WaitForSeconds objesi
    private WaitForSeconds attackDelay;

    private void Awake()
    {
        Debug.Log("[SERVER ATTACK Turret] Awake çaðrýldý.");
        mySoldierInfo = GetComponent<Turret>(); // Kendi Soldier script'imizi alýyoruz.
        myTargetDetector = GetComponentInChildren<TargetDetector>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("[SERVER ATTACK Turret] OnNetworkSpawn çaðrýldý.");
        if (!IsServer)
        {
            this.enabled = false;
            Debug.Log("[SERVER ATTACK Turret] Client: Script kapatýldý.");
            return;
        }
        Debug.Log("[SERVER ATTACK Turret] Sunucuda çalýþýyor.");
        // Yalnýzca Sunucuda: fireRate deðerine göre attackDelay'i hesapla ve ayarla.
        if (fireRate > 0)
        {
            // Örn: fireRate 2 ise (saniyede 2 atýþ), bekleme süresi 1 / 2 = 0.5 saniye olmalý.
            float delayTime = 1f / fireRate;
            attackDelay = new WaitForSeconds(delayTime);
            Debug.Log($"[SERVER ATTACK Turret] Saldýrý Gecikmesi Ayarlandý: {delayTime} saniye. FireRate: {fireRate}");
        }
        else
        {
            // fireRate 0 veya negatif ise, hata oluþmamasý için varsayýlan bir deðer verilir.
            attackDelay = new WaitForSeconds(0.1f);
            Debug.LogError("[SERVER ATTACK Turret] FireRate sýfýr veya negatif! Varsayýlan gecikme (0.1s) kullanýlýyor.");
        }
    }
    public Transform GetCurrentTarget()
    {
        Debug.Log($"[SERVER ATTACK Turret] GetCurrentTarget çaðrýldý. Mevcut Hedef: {(myCurrentTarget != null ? myCurrentTarget.name : "Yok")}");
        return myCurrentTarget;
    }


    /// <summary>
    /// Saldýrý döngüsünü baþlatýr. (Örn: Hedef menzile girdiðinde SoldiersControllerNavMesh çaðýrabilir)
    /// </summary>
    public void StartAttacking(Transform currentTarget)
    {
        Debug.Log($"[SERVER ATTACK Turret] StartAttacking çaðrýldý. Hedef: {(currentTarget != null ? currentTarget.name : "Null")}");
        // Zaten bir saldýrý döngüsü çalýþýyorsa tekrar baþlatma.
        if (attackCoroutine != null)
        {
            Debug.Log("[SERVER ATTACK Turret] Saldýrý zaten çalýþýyor, tekrar baþlatýlmýyor.");
            return;
        }
        myCurrentTarget = currentTarget;
        // Saldýrý Coroutine'ini baþlat.
        attackCoroutine = StartCoroutine(AttackCoroutine(currentTarget));
        Debug.Log("[SERVER ATTACK Turret] AttackCoroutine baþlatýldý.");

    }

    /// <summary>
    /// Saldýrý döngüsünü durdurur. (Örn: Hedef yok olduðunda/menzilden çýktýðýnda çaðrýlabilir)
    /// </summary>
    public void StopAttacking()
    {
        Debug.Log("[SERVER ATTACK Turret] StopAttacking çaðrýldý.");
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null; // Coroutine'i durdurduktan sonra referansý temizle.
            myCurrentTarget = null; // Hedef bilgisini de temizle
            Debug.Log("[SERVER ATTACK Turret] AttackCoroutine durduruldu ve referanslar temizlendi.");
        }
        else
        {
            Debug.Log("[SERVER ATTACK Turret] Durdurulacak aktif bir Coroutine yoktu.");
        }
    }

    /// <summary>
    /// Coroutine: Belirlenen atýþ hýzýna göre sürekli ateþ eder.
    /// </summary>
    private IEnumerator AttackCoroutine(Transform currentTarget)
    {
        Debug.Log("[SERVER ATTACK Turret] AttackCoroutine baþladý.");
        while (true) // Coroutine'i StopAttacking() çaðrýlana kadar sonsuza kadar çalýþtýr.
        {
            // 1. HEDEF VARLIÐI VE GEÇERLÝLÝK KONTROLÜ
            if (myCurrentTarget == null || (myCurrentTarget.TryGetComponent<NetworkObject>(out var netObj) && !netObj.IsSpawned))
            {
                Debug.LogWarning("[SERVER ATTACK Turret] Hedef yok/öldü/geçersiz. Saldýrý durduruluyor.");
                // Hedef yoksa/öldüyse: Saldýrýyý durdur ve Detector'a yeni hedef seçmesini söyle.
                StopAttacking();
                if (myTargetDetector != null)
                {
                    Debug.Log("[SERVER ATTACK Turret] Detector'a AssignBestTarget çaðrýlýyor.");
                    // Detector, mevcut listedeki ölü hedefleri temizleyecek ve yeni hedef seçecek.
                    myTargetDetector.AssignBestTarget();
                }
                yield break; // Coroutine'i bitir.
            }

            // 2. Menzil kontrolü ve saldýrý mantýðý.
            float distanceToTarget = Vector3.Distance(transform.position, myCurrentTarget.position);
            Debug.Log($"[SERVER ATTACK Turret] Hedefe olan mesafe: {distanceToTarget}. Menzil: {attackRange}");

            if (distanceToTarget <= attackRange)
            {
                Debug.Log("[SERVER ATTACK Turret] Menzilde. Hedefe bakýlýyor ve Attack() çaðrýlýyor.");
                // Menzildeyse: Yönünü hedefe çevir ve ateþ et.
                transform.LookAt(myCurrentTarget.position);
                Attack();
            }
            else
            {
                Debug.Log("[SERVER ATTACK Turret] Menzil dýþýnda, ateþ edilmiyor.");
            }
            // Hedeften çýkarsa, bu Coroutine devam eder ama ateþ etmez.

            // 3. Belirlenen atýþ hýzý kadar bekle.
            yield return attackDelay;
            Debug.Log("[SERVER ATTACK Turret] Saldýrý gecikmesi bitti, tekrar döngüye giriliyor.");
        }
    }

    /// <summary>
    /// Mermi oluþturma ve network'te spawn etme iþlemini yapar.
    /// </summary>
    private void Attack()
    {
        Debug.Log("[SERVER ATTACK Turret] Attack metodu çaðrýldý.");
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("[SERVER ATTACK Turret] Bullet Prefab veya Fire Point atanmamýþ! Durduruluyor.");
            // Hata varsa Coroutine'i durdurmak mantýklý olabilir.
            StopAttacking();
            return;
        }

        // 1. Mermiyi server'da oluþtur.
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Debug.Log("[SERVER ATTACK Turret] Mermi server'da Instantiate edildi.");

        // 2. Merminin script'ini al ve GÖREVÝNÝ VER (takým kimliði ve hasar).
        // NOT: TestBullet sýnýfý ve Initialize metodu varsayýlmýþtýr.
        TestBullet bulletScript = bulletInstance.GetComponent<TestBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(mySoldierInfo.TeamId.Value, damage);
            Debug.Log($"[SERVER ATTACK Turret] Mermi Initialize edildi. Takým: {mySoldierInfo.TeamId.Value}, Hasar: {damage}");
        }
        else
        {
            Debug.LogError("[SERVER ATTACK Turret] Mermi Prefab'ýnda TestBullet script'i bulunamadý!");
        }

        // 3. Mermiyi network'te spawn et ki client'lar da görsün.
        NetworkObject netObj = bulletInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
            Debug.Log("[SERVER ATTACK Turret] Mermi Network'te Spawn edildi.");
        }
        else
        {
            Debug.LogError("[SERVER ATTACK Turret] Mermi Prefab'ýnda NetworkObject bulunamadý!");
        }
    }
}
