using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class Soldier : NetworkBehaviour, IDamageable
{
    public Player_Game_Mode_Manager.PlayerAge age;
    [SerializeField] private float MaxHealth = 100f;



    // Health deðerini aðda senkronize tutuyoruz
    private NetworkVariable<float> myHealth = new NetworkVariable<float>(
        100f,  // varsayýlan deðer
        NetworkVariableReadPermission.Everyone,  // herkes okuyabilir
        NetworkVariableWritePermission.Server    // sadece server yazabilir
    );

 

    [Header("Unity Stuff")]
    public Image healthBar;

    [SerializeField] private float myCost;
    [SerializeField] private float myPrize;
    [SerializeField] private Transform myBarrel;
    [SerializeField] private float myRange;

    // Takým ID'sini bu deðiþkende saklayacaðýz.
    private int myTeamId = -1;

    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private TargetDetector targetDetector;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable'larýn senkronizasyonu tamamlandýðýnda bu metot çalýþýr.

        // 1. TeamId'yi GÜVENLÝ bir þekilde al ve sakla
        var unitIdentity = GetComponent<UnitIdentity>();
        if (unitIdentity != null)
        {
            myTeamId = unitIdentity.TeamId.Value;
            
        }
        else
        {
            Debug.LogError("Bu objenin üzerinde UnitIdentity component'i bulunamadý!", gameObject);
        }


        // 2. Sadece Server üzerinde çalýþacak mantýk
        if (IsServer)
        {
            myHealth.Value = MaxHealth;
            Debug.Log($"[SERVER] Asker spawn oldu. Takýmý: {myTeamId}", gameObject);
            gameObject.name = myTeamId.ToString();

            // Örneðin, burada takýmýna göre bir hedef bulma mantýðý çalýþtýrýlabilir.
            // FindInitialTarget(); 


            // 1. TeamId'yi GÜVENLÝ bir þekilde al ve sakla
            var soldiersControllerNavMesh = GetComponent<SoldiersControllerNavMesh>();
            if (soldiersControllerNavMesh != null)
            {
                // Coroutine'i bu þekilde baþlatmalýsýn:
                StartCoroutine(soldiersControllerNavMesh.FindTargetAndSetDestination());
            }
            else
            {
                Debug.LogError("Bu objenin üzerinde SoldiersControllerNavMesh component'i bulunamadý!", gameObject);
            }
        }
        // 3. Tüm client'larda çalýþacak mantýk (Görsel güncellemeler vb.)
        myHealth.OnValueChanged += OnHealthChanged;
        OnHealthChanged(0, myHealth.Value); // UI'ý ilk deðerle güncelle
        if (targetDetector != null)
        {
            targetDetector.WhenNetworkSpawn();
        }
        else
        {
            Debug.LogError("Bu objenin üzerinde TargetDetector component'i bulunamadý!", gameObject);
        }

    }


    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthBar != null)
            healthBar.fillAmount = newValue / MaxHealth;
    }



    // === IDamageable implementasyonu ===
    [ServerRpc]
    public void TakeDamageServerRpc(float damageAmount)
    {
        TakeDamage(damageAmount); // interface metodunu çaðýr
    }

    public void TakeDamage(float damageAmount)
    {
     
        if (myHealth.Value <= 0) return;

        myHealth.Value -= damageAmount;
        if (myHealth.Value <= 0)
        {
            Die();
        }
       
    }

    public void Die()
    {
        gameObject.SetActive(false);
        // GetComponent<NetworkObject>().Despawn(); // alternatifi
    }


    // Soldier.cs içine bu metodu ekle
    public int GetTeamId()
    {
        return myTeamId;
    }
}
