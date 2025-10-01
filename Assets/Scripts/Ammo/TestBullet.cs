using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class TestBullet : NetworkBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 3f;

    private float damageAmount;
    private int ownerTeamId;

    // Mermiyi kimin, hangi takýmdan ve ne kadar hasarla ateþlediðini belirten baþlangýç metodu.
    // Bu metot, mermi spawn olmadan hemen önce SADECE server'da çaðrýlacak.
    public void Initialize(int teamId, float damage)
    {
        this.ownerTeamId = teamId;
        this.damageAmount = damage;
    }

    public override void OnNetworkSpawn()
    {
        // Mermi spawn olduðunda, eðer server ise ömrünü baþlat.
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    // Mermiyi yok eden metot
    private void DestroyBullet()
    {
        // Eðer hala network'te ise despawn et.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    private void Update()
    {
        // Merminin hareketi tüm client'larda ayný þekilde görünsün diye Update'te.
        // Daha geliþmiþ bir sistem için NetworkTransform de kullanýlabilir.
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Çarpýþma ve hasar mantýðý SADECE server'da çalýþmalý.
        if (!IsServer) return;

        // Hasarýmýz zaten sýfýrlandýysa veya bir þekil2de mermi geçersizse bir þey yapma.
        if (damageAmount <= 0) return;

        // Çarptýðýmýz objenin kimlik bilgisi var mý?
        var targetIdentity = other.GetComponent<UnitIdentity>();
        if (targetIdentity != null)
        {
            // Eðer çarptýðýmýz þey kendi takýmýmýzdansa, hasar verme ve yok ol.
            if (targetIdentity.TeamId.Value == ownerTeamId)
            {
                // Kendi takým arkadaþýna çarptýn. Hasar verme.
                DestroyBullet();
                return; // Metodun devamýný çalýþtýrma.
            }
        }

        // Çarptýðýmýz þeyin caný var mý? (IDamageable)
        var damageableTarget = other.GetComponent<IDamageable>();
        if (damageableTarget != null)
        {
            // Düþmana çarptýk! Hasar ver.
            damageableTarget.TakeDamage(damageAmount);
        }

        // Mermi bir þeye çarptýðý için (düþman, dost, duvar fark etmez) görevini tamamladý.
        // Hasarýný sýfýrla ve kendini yok et.
        damageAmount = 0;
        DestroyBullet();
    }



}
