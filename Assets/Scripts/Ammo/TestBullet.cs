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
        Debug.Log($"[SERVER/BULLET INIT] Mermi {gameObject.name} baþlatýldý. Takým ID: {teamId}, Hasar: {damage}");
        this.ownerTeamId = teamId;
        this.damageAmount = damage;
    }

    public override void OnNetworkSpawn()
    {
        // Mermi spawn olduðunda, eðer server ise ömrünü baþlat.
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifetime);
            Debug.Log($"[SERVER/BULLET SPAWN] Mermi {gameObject.name} Network'te spawn oldu. Ömür (lifetime): {lifetime}s. Takým: {ownerTeamId}");
        }
        else // Client tarafý için debug (opsiyonel)
        {
            Debug.Log($"[CLIENT/BULLET SPAWN] Mermi {gameObject.name} client'ta spawn edildi. ownerTeamId: {ownerTeamId}");
        }
    }

    // Mermiyi yok eden metot
    private void DestroyBullet()
    {
        // GÜNCELLENMÝÞ DEBUG LOG: Yok etme kararý
        Debug.Log($"[SERVER/BULLET DESTROY] Mermi {gameObject.name} Despawn ediliyor (Ömür bitti).");
        // Eðer hala network'te ise despawn et.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            // NetworkObject zaten yoksa veya despawn edilmiþse (örneðin çarpýþmadan dolayý)
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
            // YENÝ DEBUG LOG: Çarpýþma baþlangýcý
            Debug.Log($"[SERVER/BULLET HIT] Mermi {gameObject.name} çarpýþtý. Çarpan: {other.gameObject.name} | Hasar: {damageAmount} | Sahip Takým: {ownerTeamId}");
            // Hasarýmýz zaten sýfýrlandýysa veya bir þekil2de mermi geçersizse bir þey yapma.
            if (damageAmount <= 0) 
            {
                Debug.Log($"[SERVER/BULLET HIT] Mermi {gameObject.name} çarpýþtý ancak hasar zaten sýfýrlanmýþ. Ýþlem iptal.");
                return;
            }

            if (other.gameObject.name == "TargetDetector" || other.gameObject.name == "bullet(Clone)")
            {
                Debug.Log($"[SERVER/BULLET HIT] Mermi {gameObject.name}, {other.gameObject.name} ile çarpýþtý. Göz ardý ediliyor.");
                return;
            }
            // Çarptýðýmýz objenin kimlik bilgisi var mý?
            var targetIdentity = other.GetComponent<Soldier>();
            if (targetIdentity != null)
            {
                Debug.Log($"[SERVER/BULLET HIT] Hedef birim bulundu. Hedef Takým ID: {targetIdentity.TeamId.Value}");
                // Eðer çarptýðýmýz þey kendi takýmýmýzdansa, hasar verme ve yok ol.
                if (targetIdentity.TeamId.Value == ownerTeamId )
                {
                    // Kendi takým arkadaþýna çarptýn. Hasar verme.
                    Debug.Log($"[SERVER/BULLET HIT] Kendi takým arkadaþýna ({other.name}) çarptý. Hasar verilmeyecek, mermi yoluna devam etsin.");
                    
                    return; // Metodun devamýný çalýþtýrma.
                }
            }

            // Çarptýðýmýz þeyin caný var mý? (IDamageable)
            var damageableTarget = other.GetComponent<IDamageable>();
            if (damageableTarget != null)
            {
                Debug.Log($"[SERVER/BULLET HIT] Hasar verilebilir düþmana ({other.name}) çarptý. {damageAmount} hasar veriliyor.");
                // Düþmana çarptýk! Hasar ver.
                damageableTarget.TakeDamage(damageAmount);
            }
            else
            {
                // Düþman olmayan bir þeye çarptý (örneðin duvar, arazi, base vb.)
                Debug.Log($"[SERVER/BULLET HIT] Hasar verilemez bir cisme ({other.name}) çarptý.");
            }

            // Mermi bir þeye çarptýðý için (düþman, dost, duvar fark etmez) görevini tamamladý.
            // Hasarýný sýfýrla ve kendini yok et.
            damageAmount = 0;
            
            DestroyBullet();
        }
    }
}
