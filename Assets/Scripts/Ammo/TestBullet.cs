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
        this.ownerTeamId = teamId;
        this.damageAmount = damage;
    }

    public override void OnNetworkSpawn()
    {
        // Mermi spawn oldu�unda, e�er server ise �mr�n� ba�lat.
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    // Mermiyi yok eden metot
    private void DestroyBullet()
    {
        // E�er hala network'te ise despawn et.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
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
        if (!IsServer) return;

        // Hasar�m�z zaten s�f�rland�ysa veya bir �ekil2de mermi ge�ersizse bir �ey yapma.
        if (damageAmount <= 0) return;

        // �arpt���m�z objenin kimlik bilgisi var m�?
        var targetIdentity = other.GetComponent<UnitIdentity>();
        if (targetIdentity != null)
        {
            // E�er �arpt���m�z �ey kendi tak�m�m�zdansa, hasar verme ve yok ol.
            if (targetIdentity.TeamId.Value == ownerTeamId)
            {
                // Kendi tak�m arkada��na �arpt�n. Hasar verme.
                DestroyBullet();
                return; // Metodun devam�n� �al��t�rma.
            }
        }

        // �arpt���m�z �eyin can� var m�? (IDamageable)
        var damageableTarget = other.GetComponent<IDamageable>();
        if (damageableTarget != null)
        {
            // D��mana �arpt�k! Hasar ver.
            damageableTarget.TakeDamage(damageAmount);
        }

        // Mermi bir �eye �arpt��� i�in (d��man, dost, duvar fark etmez) g�revini tamamlad�.
        // Hasar�n� s�f�rla ve kendini yok et.
        damageAmount = 0;
        DestroyBullet();
    }



}
