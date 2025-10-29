using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Soldier))] // Bu script'in çalýþmasý için bir Soldier script'i gerektiðini belirtir.
[RequireComponent(typeof(SoldiersControllerNavMesh))]
public class SoldiersAttackController : NetworkBehaviour
{
    [Header("Saldýrý Ayarlarý")]
    private float attackRange = 9999f;
    [SerializeField] private float fireRate = 1f; // Saniyede kaç atýþ
    [SerializeField] private float damage = 10f;

    [Header("Mermi Ayarlarý")]
    [SerializeField] private GameObject bulletPrefab; // Inspector'dan mermi prefab'ýný sürükle
    [SerializeField] private Transform firePoint; // Merminin çýkacaðý yer (namlu ucu vb.)


    private Soldier mySoldierInfo;
    private TargetDetector myTargetDetector; // YENÝ: TargetDetector referansý
    private Transform myCurrentTarget;

    private Coroutine attackCoroutine; // Çalýþan Coroutine'e referans tutar.

    // Gecikme hesaplamasý için önceden tanýmlanmýþ bir WaitForSeconds objesi
    private WaitForSeconds attackDelay ;
    // Bu deðiþkeni 3 script'e de ekle (Soldier, SoldiersControllerNavMesh, SoldiersAttackController)
    [SerializeField]private Animator modelAnimator;
    private void Awake()
    {
        mySoldierInfo = GetComponent<Soldier>(); // Kendi Soldier script'imizi alýyoruz.
        myTargetDetector = GetComponentInChildren<TargetDetector>();
        attackRange= myTargetDetector.GetComponent<SphereCollider>().radius;
        // Kendi objemizdeki deðil, "child" objelerdeki Animator'ü bul
        //modelAnimator = GetComponentInChildren<Animator>();

        if (modelAnimator == null)
        {
            Debug.LogError("Child objede Animator component'i bulunamadý!", gameObject);
        }
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
    public Transform GetCurrentTarget() 
    {
        return myCurrentTarget;
    }


    /// <summary>
    /// Saldýrý döngüsünü baþlatýr. (Örn: Hedef menzile girdiðinde SoldiersControllerNavMesh çaðýrabilir)
    /// </summary>
    public void StartAttacking(Transform currentTarget)
    {
        // Zaten bir saldýrý döngüsü çalýþýyorsa tekrar baþlatma.
        if (attackCoroutine != null) return;
        myCurrentTarget = currentTarget;
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
            myCurrentTarget = null; // Hedef bilgisini de temizle
        }
    }

    /// <summary>
    /// Coroutine: Belirlenen atýþ hýzýna göre sürekli ateþ eder.
    /// </summary>
    private IEnumerator AttackCoroutine(Transform currentTarget)
    {
        while (true) // Coroutine'i StopAttacking() çaðrýlana kadar sonsuza kadar çalýþtýr.
        {


            // 1. HEDEF VARLIÐI VE GEÇERLÝLÝK KONTROLÜ (YENÝ EKLEME)
            // myCurrentTarget == null (Unity objesi destroy edildi) VEYA 
            // NetworkObject == null (Unit Netcode tarafýndan Despawn edildi)
            if (myCurrentTarget == null || (myCurrentTarget.TryGetComponent<NetworkObject>(out var netObj) && !netObj.IsSpawned))
            {
                // Hedef yoksa/öldüyse: Saldýrýyý durdur ve Detector'a yeni hedef seçmesini söyle.
                StopAttacking();
                if (myTargetDetector != null)
                {
                    // Detector, mevcut listedeki ölü hedefleri temizleyecek ve yeni hedef seçecek.
                    myTargetDetector.AssignBestTarget();
                }
                yield break; // Coroutine'i bitir.
            }

            // 2. Menzil kontrolü ve saldýrý mantýðý.
            float distanceToTarget = Vector3.Distance(transform.position, myCurrentTarget.position); // myCurrentTarget kullanýldý

            if (distanceToTarget <= attackRange)
            {
                // Menzildeyse: Yönünü hedefe çevir ve ateþ et.
                transform.LookAt(myCurrentTarget.position);
                Attack();
            }
            // Hedeften çýkarsa, bu Coroutine devam eder ama ateþ etmez.

            // 3. Belirlenen atýþ hýzý kadar bekle.
            yield return attackDelay;
        }
    }

    /// <summary>
    /// Mermi oluþturma ve network'te spawn etme iþlemini yapar.
    /// </summary>
    private void Attack()
    {
        // --- YENÝ EKLENEN KISIM ---
        if (modelAnimator != null)
        {
            // 'Attack' trigger'ýný ateþle.
            // NetworkAnimator bu tetiklemeyi tüm client'lara gönderecek.
            modelAnimator.SetTrigger("Attack");
            Debug.Log("[SERVER ATTACK] Attack animasyonu tetiklendi.");
        }
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
