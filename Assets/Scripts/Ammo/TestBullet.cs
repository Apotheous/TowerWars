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

    // Mermiyi kimin, hangi tak�mdan ve ne kadar hasarla ate�ledi�ini belirten ba�lang�� metodu.
    // Bu metot, mermi spawn olmadan hemen �nce SADECE server'da �a�r�lacak.
    public void Initialize(int teamId, float damage)
    {
        Debug.Log($"[SERVER/BULLET INIT] Mermi {gameObject.name} ba�lat�ld�. Tak�m ID: {teamId}, Hasar: {damage}");
        this.ownerTeamId = teamId;
        this.damageAmount = damage;
    }

    public override void OnNetworkSpawn()
    {
        // Mermi spawn oldu�unda, e�er server ise �mr�n� ba�lat.
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifetime);
            Debug.Log($"[SERVER/BULLET SPAWN] Mermi {gameObject.name} Network'te spawn oldu. �m�r (lifetime): {lifetime}s. Tak�m: {ownerTeamId}");
        }
        else // Client taraf� i�in debug (opsiyonel)
        {
            Debug.Log($"[CLIENT/BULLET SPAWN] Mermi {gameObject.name} client'ta spawn edildi. ownerTeamId: {ownerTeamId}");
        }
    }

    // Mermiyi yok eden metot
    private void DestroyBullet()
    {
        // G�NCELLENM�� DEBUG LOG: Yok etme karar�
        Debug.Log($"[SERVER/BULLET DESTROY] Mermi {gameObject.name} Despawn ediliyor (�m�r bitti).");
        // E�er hala network'te ise despawn et.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            // NetworkObject zaten yoksa veya despawn edilmi�se (�rne�in �arp��madan dolay�)
            Destroy(gameObject);
        }

    }

    private void Update()
    {
        // Merminin hareketi t�m client'larda ayn� �ekilde g�r�ns�n diye Update'te.
        // Daha geli�mi� bir sistem i�in NetworkTransform de kullan�labilir.
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // �arp��ma ve hasar mant��� SADECE server'da �al��mal�.
        if (IsServer)
        {
            // YEN� DEBUG LOG: �arp��ma ba�lang�c�
            Debug.Log($"[SERVER/BULLET HIT] Mermi {gameObject.name} �arp��t�. �arpan: {other.gameObject.name} | Hasar: {damageAmount} | Sahip Tak�m: {ownerTeamId}");
            // Hasar�m�z zaten s�f�rland�ysa veya bir �ekil2de mermi ge�ersizse bir �ey yapma.
            if (damageAmount <= 0) 
            {
                Debug.Log($"[SERVER/BULLET HIT] Mermi {gameObject.name} �arp��t� ancak hasar zaten s�f�rlanm��. ��lem iptal.");
                return;
            }

            if (other.gameObject.name == "TargetDetector" || other.gameObject.name == "bullet(Clone)")
            {
                Debug.Log($"[SERVER/BULLET HIT] Mermi {gameObject.name}, {other.gameObject.name} ile �arp��t�. G�z ard� ediliyor.");
                return;
            }
            // �arpt���m�z objenin kimlik bilgisi var m�?
            var targetIdentity = other.GetComponent<Soldier>();
            if (targetIdentity != null)
            {
                Debug.Log($"[SERVER/BULLET HIT] Hedef birim bulundu. Hedef Tak�m ID: {targetIdentity.TeamId.Value}");
                // E�er �arpt���m�z �ey kendi tak�m�m�zdansa, hasar verme ve yok ol.
                if (targetIdentity.TeamId.Value == ownerTeamId )
                {
                    // Kendi tak�m arkada��na �arpt�n. Hasar verme.
                    Debug.Log($"[SERVER/BULLET HIT] Kendi tak�m arkada��na ({other.name}) �arpt�. Hasar verilmeyecek, mermi yoluna devam etsin.");
                    
                    return; // Metodun devam�n� �al��t�rma.
                }
            }

            // �arpt���m�z �eyin can� var m�? (IDamageable)
            var damageableTarget = other.GetComponent<IDamageable>();
            if (damageableTarget != null)
            {
                Debug.Log($"[SERVER/BULLET HIT] Hasar verilebilir d��mana ({other.name}) �arpt�. {damageAmount} hasar veriliyor.");
                // D��mana �arpt�k! Hasar ver.
                damageableTarget.TakeDamage(damageAmount);
            }
            else
            {
                // D��man olmayan bir �eye �arpt� (�rne�in duvar, arazi, base vb.)
                Debug.Log($"[SERVER/BULLET HIT] Hasar verilemez bir cisme ({other.name}) �arpt�.");
            }

            // Mermi bir �eye �arpt��� i�in (d��man, dost, duvar fark etmez) g�revini tamamlad�.
            // Hasar�n� s�f�rla ve kendini yok et.
            damageAmount = 0;
            
            DestroyBullet();
        }
    }
}
