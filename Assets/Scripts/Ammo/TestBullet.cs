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
        // Önemli: Yok etme iþlemi baþlamadan önce Invoke'u iptal et.
        // Bu, çarpýþma ile yok etme ve ömürle yok etme arasýndaki çakýþmayý önler.
        // NetworkObject null kontrolü eklendi, çünkü despawn sonrasý null olabilir.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            // Ýptal iþlemi sadece Server'da yapýlmalýdýr, Client'ta zaten zamanlanmýþ çaðrý yoktur.
            if (IsServer)
            {
                CancelInvoke(nameof(DestroyBullet)); // Kalan Invoke'u iptal et
                NetworkObject.Despawn();
            }
        }
        else
        {
            // Network'te deðilse direkt yok et.
            Destroy(gameObject);
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
        if (IsServer)
        {
            // Hasarýmýz zaten sýfýrlandýysa veya bir þekil2de mermi geçersizse bir þey yapma.
            if (damageAmount <= 0) 
            {
                return;
            }

            if (other.gameObject.name == "TargetDetector" || other.gameObject.name == "bullet(Clone)")
            {
                return;
            }
            // Çarptýðýmýz objenin kimlik bilgisi var mý?
            var targetIdentity = other.GetComponent<Soldier>();
            if (targetIdentity != null)
            {
                // Eðer çarptýðýmýz þey kendi takýmýmýzdansa, hasar verme ve yok ol.
                if (targetIdentity.TeamId.Value == ownerTeamId )
                {
                    return; // Metodun devamýný çalýþtýrma.
                }
            }

            // Çarptýðýmýz þeyin caný var mý? (IDamageable)
            var damageableTarget = other.GetComponent<IDamageable>();
            if (damageableTarget != null)
            {
                damageableTarget.TakeDamage(damageAmount);
            }
     
            damageAmount = 0;
            CancelInvoke(nameof(DestroyBullet));
            DestroyBullet();
        }
    }
}
