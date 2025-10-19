using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



[RequireComponent(typeof(Turret))] // Bu script'in �al��mas� i�in bir Soldier script'i gerekti�ini belirtir.
[RequireComponent(typeof(TurretsRotationController))]
public class TurretsAttackController : NetworkBehaviour
{
    [Header("Sald�r� Ayarlar�")]
    private float attackRange = 9999f;
    [SerializeField] private float fireRate = 1f; // Saniyede ka� at��
    [SerializeField] private float damage = 10f;

    [Header("Mermi Ayarlar�")]
    [SerializeField] private GameObject bulletPrefab; // Inspector'dan mermi prefab'�n� s�r�kle
    [SerializeField] private Transform firePoint; // Merminin ��kaca�� yer (namlu ucu vb.)


    private Turret mySoldierInfo;//Burada Kald�m
    private TargetDetector myTargetDetector; // YEN�: TargetDetector referans�
    private Transform myCurrentTarget;

    private Coroutine attackCoroutine; // �al��an Coroutine'e referans tutar.

    // Gecikme hesaplamas� i�in �nceden tan�mlanm�� bir WaitForSeconds objesi
    private WaitForSeconds attackDelay;

    private void Awake()
    {
        Debug.Log("[SERVER ATTACK Turret] Awake �a�r�ld�.");
        mySoldierInfo = GetComponent<Turret>(); // Kendi Soldier script'imizi al�yoruz.
        myTargetDetector = GetComponentInChildren<TargetDetector>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("[SERVER ATTACK Turret] OnNetworkSpawn �a�r�ld�.");
        if (!IsServer)
        {
            this.enabled = false;
            Debug.Log("[SERVER ATTACK Turret] Client: Script kapat�ld�.");
            return;
        }
        Debug.Log("[SERVER ATTACK Turret] Sunucuda �al���yor.");
        // Yaln�zca Sunucuda: fireRate de�erine g�re attackDelay'i hesapla ve ayarla.
        if (fireRate > 0)
        {
            // �rn: fireRate 2 ise (saniyede 2 at��), bekleme s�resi 1 / 2 = 0.5 saniye olmal�.
            float delayTime = 1f / fireRate;
            attackDelay = new WaitForSeconds(delayTime);
            Debug.Log($"[SERVER ATTACK Turret] Sald�r� Gecikmesi Ayarland�: {delayTime} saniye. FireRate: {fireRate}");
        }
        else
        {
            // fireRate 0 veya negatif ise, hata olu�mamas� i�in varsay�lan bir de�er verilir.
            attackDelay = new WaitForSeconds(0.1f);
            Debug.LogError("[SERVER ATTACK Turret] FireRate s�f�r veya negatif! Varsay�lan gecikme (0.1s) kullan�l�yor.");
        }
    }
    public Transform GetCurrentTarget()
    {
        Debug.Log($"[SERVER ATTACK Turret] GetCurrentTarget �a�r�ld�. Mevcut Hedef: {(myCurrentTarget != null ? myCurrentTarget.name : "Yok")}");
        return myCurrentTarget;
    }


    /// <summary>
    /// Sald�r� d�ng�s�n� ba�lat�r. (�rn: Hedef menzile girdi�inde SoldiersControllerNavMesh �a��rabilir)
    /// </summary>
    public void StartAttacking(Transform currentTarget)
    {
        Debug.Log($"[SERVER ATTACK Turret] StartAttacking �a�r�ld�. Hedef: {(currentTarget != null ? currentTarget.name : "Null")}");
        // Zaten bir sald�r� d�ng�s� �al���yorsa tekrar ba�latma.
        if (attackCoroutine != null)
        {
            Debug.Log("[SERVER ATTACK Turret] Sald�r� zaten �al���yor, tekrar ba�lat�lm�yor.");
            return;
        }
        myCurrentTarget = currentTarget;
        // Sald�r� Coroutine'ini ba�lat.
        attackCoroutine = StartCoroutine(AttackCoroutine(currentTarget));
        Debug.Log("[SERVER ATTACK Turret] AttackCoroutine ba�lat�ld�.");

    }

    /// <summary>
    /// Sald�r� d�ng�s�n� durdurur. (�rn: Hedef yok oldu�unda/menzilden ��kt���nda �a�r�labilir)
    /// </summary>
    public void StopAttacking()
    {
        Debug.Log("[SERVER ATTACK Turret] StopAttacking �a�r�ld�.");
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null; // Coroutine'i durdurduktan sonra referans� temizle.
            myCurrentTarget = null; // Hedef bilgisini de temizle
            Debug.Log("[SERVER ATTACK Turret] AttackCoroutine durduruldu ve referanslar temizlendi.");
        }
        else
        {
            Debug.Log("[SERVER ATTACK Turret] Durdurulacak aktif bir Coroutine yoktu.");
        }
    }

    /// <summary>
    /// Coroutine: Belirlenen at�� h�z�na g�re s�rekli ate� eder.
    /// </summary>
    private IEnumerator AttackCoroutine(Transform currentTarget)
    {
        Debug.Log("[SERVER ATTACK Turret] AttackCoroutine ba�lad�.");
        while (true) // Coroutine'i StopAttacking() �a�r�lana kadar sonsuza kadar �al��t�r.
        {
            // 1. HEDEF VARLI�I VE GE�ERL�L�K KONTROL�
            if (myCurrentTarget == null || (myCurrentTarget.TryGetComponent<NetworkObject>(out var netObj) && !netObj.IsSpawned))
            {
                Debug.LogWarning("[SERVER ATTACK Turret] Hedef yok/�ld�/ge�ersiz. Sald�r� durduruluyor.");
                // Hedef yoksa/�ld�yse: Sald�r�y� durdur ve Detector'a yeni hedef se�mesini s�yle.
                StopAttacking();
                if (myTargetDetector != null)
                {
                    Debug.Log("[SERVER ATTACK Turret] Detector'a AssignBestTarget �a�r�l�yor.");
                    // Detector, mevcut listedeki �l� hedefleri temizleyecek ve yeni hedef se�ecek.
                    myTargetDetector.AssignBestTarget();
                }
                yield break; // Coroutine'i bitir.
            }

            // 2. Menzil kontrol� ve sald�r� mant���.
            float distanceToTarget = Vector3.Distance(transform.position, myCurrentTarget.position);
            Debug.Log($"[SERVER ATTACK Turret] Hedefe olan mesafe: {distanceToTarget}. Menzil: {attackRange}");

            if (distanceToTarget <= attackRange)
            {
                Debug.Log("[SERVER ATTACK Turret] Menzilde. Hedefe bak�l�yor ve Attack() �a�r�l�yor.");
                // Menzildeyse: Y�n�n� hedefe �evir ve ate� et.
                transform.LookAt(myCurrentTarget.position);
                Attack();
            }
            else
            {
                Debug.Log("[SERVER ATTACK Turret] Menzil d���nda, ate� edilmiyor.");
            }
            // Hedeften ��karsa, bu Coroutine devam eder ama ate� etmez.

            // 3. Belirlenen at�� h�z� kadar bekle.
            yield return attackDelay;
            Debug.Log("[SERVER ATTACK Turret] Sald�r� gecikmesi bitti, tekrar d�ng�ye giriliyor.");
        }
    }

    /// <summary>
    /// Mermi olu�turma ve network'te spawn etme i�lemini yapar.
    /// </summary>
    private void Attack()
    {
        Debug.Log("[SERVER ATTACK Turret] Attack metodu �a�r�ld�.");
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("[SERVER ATTACK Turret] Bullet Prefab veya Fire Point atanmam��! Durduruluyor.");
            // Hata varsa Coroutine'i durdurmak mant�kl� olabilir.
            StopAttacking();
            return;
        }

        // 1. Mermiyi server'da olu�tur.
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Debug.Log("[SERVER ATTACK Turret] Mermi server'da Instantiate edildi.");

        // 2. Merminin script'ini al ve G�REV�N� VER (tak�m kimli�i ve hasar).
        // NOT: TestBullet s�n�f� ve Initialize metodu varsay�lm��t�r.
        TestBullet bulletScript = bulletInstance.GetComponent<TestBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(mySoldierInfo.TeamId.Value, damage);
            Debug.Log($"[SERVER ATTACK Turret] Mermi Initialize edildi. Tak�m: {mySoldierInfo.TeamId.Value}, Hasar: {damage}");
        }
        else
        {
            Debug.LogError("[SERVER ATTACK Turret] Mermi Prefab'�nda TestBullet script'i bulunamad�!");
        }

        // 3. Mermiyi network'te spawn et ki client'lar da g�rs�n.
        NetworkObject netObj = bulletInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
            Debug.Log("[SERVER ATTACK Turret] Mermi Network'te Spawn edildi.");
        }
        else
        {
            Debug.LogError("[SERVER ATTACK Turret] Mermi Prefab'�nda NetworkObject bulunamad�!");
        }
    }
}
