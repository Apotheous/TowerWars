using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Soldier))] // Bu script'in �al��mas� i�in bir Soldier script'i gerekti�ini belirtir.
[RequireComponent(typeof(SoldiersControllerNavMesh))]
public class SoldiersAttackController : NetworkBehaviour
{
    [Header("Sald�r� Ayarlar�")]
    private float attackRange = 9999f;
    [SerializeField] private float fireRate = 1f; // Saniyede ka� at��
    [SerializeField] private float damage = 10f;

    [Header("Mermi Ayarlar�")]
    [SerializeField] private GameObject bulletPrefab; // Inspector'dan mermi prefab'�n� s�r�kle
    [SerializeField] private Transform firePoint; // Merminin ��kaca�� yer (namlu ucu vb.)


    private Soldier mySoldierInfo;
    private TargetDetector myTargetDetector; // YEN�: TargetDetector referans�
    private Transform myCurrentTarget;

    private Coroutine attackCoroutine; // �al��an Coroutine'e referans tutar.

    // Gecikme hesaplamas� i�in �nceden tan�mlanm�� bir WaitForSeconds objesi
    private WaitForSeconds attackDelay ;
    // Bu de�i�keni 3 script'e de ekle (Soldier, SoldiersControllerNavMesh, SoldiersAttackController)
    [SerializeField]private Animator modelAnimator;
    private void Awake()
    {
        mySoldierInfo = GetComponent<Soldier>(); // Kendi Soldier script'imizi al�yoruz.
        myTargetDetector = GetComponentInChildren<TargetDetector>();
        attackRange= myTargetDetector.GetComponent<SphereCollider>().radius;
        // Kendi objemizdeki de�il, "child" objelerdeki Animator'� bul
        //modelAnimator = GetComponentInChildren<Animator>();

        if (modelAnimator == null)
        {
            Debug.LogError("Child objede Animator component'i bulunamad�!", gameObject);
        }
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
    public Transform GetCurrentTarget() 
    {
        return myCurrentTarget;
    }


    /// <summary>
    /// Sald�r� d�ng�s�n� ba�lat�r. (�rn: Hedef menzile girdi�inde SoldiersControllerNavMesh �a��rabilir)
    /// </summary>
    public void StartAttacking(Transform currentTarget)
    {
        // Zaten bir sald�r� d�ng�s� �al���yorsa tekrar ba�latma.
        if (attackCoroutine != null) return;
        myCurrentTarget = currentTarget;
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
            myCurrentTarget = null; // Hedef bilgisini de temizle
        }
    }

    /// <summary>
    /// Coroutine: Belirlenen at�� h�z�na g�re s�rekli ate� eder.
    /// </summary>
    private IEnumerator AttackCoroutine(Transform currentTarget)
    {
        while (true) // Coroutine'i StopAttacking() �a�r�lana kadar sonsuza kadar �al��t�r.
        {


            // 1. HEDEF VARLI�I VE GE�ERL�L�K KONTROL� (YEN� EKLEME)
            // myCurrentTarget == null (Unity objesi destroy edildi) VEYA 
            // NetworkObject == null (Unit Netcode taraf�ndan Despawn edildi)
            if (myCurrentTarget == null || (myCurrentTarget.TryGetComponent<NetworkObject>(out var netObj) && !netObj.IsSpawned))
            {
                // Hedef yoksa/�ld�yse: Sald�r�y� durdur ve Detector'a yeni hedef se�mesini s�yle.
                StopAttacking();
                if (myTargetDetector != null)
                {
                    // Detector, mevcut listedeki �l� hedefleri temizleyecek ve yeni hedef se�ecek.
                    myTargetDetector.AssignBestTarget();
                }
                yield break; // Coroutine'i bitir.
            }

            // 2. Menzil kontrol� ve sald�r� mant���.
            float distanceToTarget = Vector3.Distance(transform.position, myCurrentTarget.position); // myCurrentTarget kullan�ld�

            if (distanceToTarget <= attackRange)
            {
                // Menzildeyse: Y�n�n� hedefe �evir ve ate� et.
                transform.LookAt(myCurrentTarget.position);
                Attack();
            }
            // Hedeften ��karsa, bu Coroutine devam eder ama ate� etmez.

            // 3. Belirlenen at�� h�z� kadar bekle.
            yield return attackDelay;
        }
    }

    /// <summary>
    /// Mermi olu�turma ve network'te spawn etme i�lemini yapar.
    /// </summary>
    private void Attack()
    {
        // --- YEN� EKLENEN KISIM ---
        if (modelAnimator != null)
        {
            // 'Attack' trigger'�n� ate�le.
            // NetworkAnimator bu tetiklemeyi t�m client'lara g�nderecek.
            modelAnimator.SetTrigger("Attack");
            Debug.Log("[SERVER ATTACK] Attack animasyonu tetiklendi.");
        }
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
