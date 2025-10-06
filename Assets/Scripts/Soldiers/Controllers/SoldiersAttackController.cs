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
    private Transform currentTarget;
    private float fireTimer;

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
    }

    private void Update()
    {
        currentTarget = movementController.GetCurrentTarget();
        if (currentTarget == null) return;

        fireTimer += Time.deltaTime;
        if (fireTimer < 1f / fireRate) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= attackRange)
        {
            // Menzildeyse ate� et! Y�n�n� hedefe �evir ve ate�le.
            transform.LookAt(currentTarget.position);
            Attack();
            fireTimer = 0f;
        }
    }

    private void Attack()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab veya Fire Point atanmam��!");
            return;
        }

        // 1. Mermiyi server'da olu�tur.
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 2. Merminin script'ini al ve G�REV�N� VER (tak�m kimli�i ve hasar).
        TestBullet bulletScript = bulletInstance.GetComponent<TestBullet>();
        bulletScript.Initialize(mySoldierInfo.TeamId.Value, damage);

        // 3. Mermiyi network'te spawn et ki client'lar da g�rs�n.
        bulletInstance.GetComponent<NetworkObject>().Spawn(true);
    }
}
