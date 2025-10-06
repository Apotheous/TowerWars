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
    private Transform currentTarget;
    private float fireTimer;

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
            // Menzildeyse ateþ et! Yönünü hedefe çevir ve ateþle.
            transform.LookAt(currentTarget.position);
            Attack();
            fireTimer = 0f;
        }
    }

    private void Attack()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Bullet Prefab veya Fire Point atanmamýþ!");
            return;
        }

        // 1. Mermiyi server'da oluþtur.
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 2. Merminin script'ini al ve GÖREVÝNÝ VER (takým kimliði ve hasar).
        TestBullet bulletScript = bulletInstance.GetComponent<TestBullet>();
        bulletScript.Initialize(mySoldierInfo.TeamId.Value, damage);

        // 3. Mermiyi network'te spawn et ki client'lar da görsün.
        bulletInstance.GetComponent<NetworkObject>().Spawn(true);
    }
}
