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
        // �nemli: Yok etme i�lemi ba�lamadan �nce Invoke'u iptal et.
        // Bu, �arp��ma ile yok etme ve �m�rle yok etme aras�ndaki �ak��may� �nler.
        // NetworkObject null kontrol� eklendi, ��nk� despawn sonras� null olabilir.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            // �ptal i�lemi sadece Server'da yap�lmal�d�r, Client'ta zaten zamanlanm�� �a�r� yoktur.
            if (IsServer)
            {
                CancelInvoke(nameof(DestroyBullet)); // Kalan Invoke'u iptal et
                NetworkObject.Despawn();
            }
        }
        else
        {
            // Network'te de�ilse direkt yok et.
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
            // Hasar�m�z zaten s�f�rland�ysa veya bir �ekil2de mermi ge�ersizse bir �ey yapma.
            if (damageAmount <= 0) 
            {
                return;
            }

            if (other.gameObject.name == "TargetDetector" || other.gameObject.name == "bullet(Clone)")
            {
                return;
            }
            // �arpt���m�z objenin kimlik bilgisi var m�?
            var targetIdentity = other.GetComponent<Soldier>();
            if (targetIdentity != null)
            {
                // E�er �arpt���m�z �ey kendi tak�m�m�zdansa, hasar verme ve yok ol.
                if (targetIdentity.TeamId.Value == ownerTeamId )
                {
                    return; // Metodun devam�n� �al��t�rma.
                }
            }

            // �arpt���m�z �eyin can� var m�? (IDamageable)
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
