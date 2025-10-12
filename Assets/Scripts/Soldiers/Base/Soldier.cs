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

    //PlayerProductionManagement ProduceNextUnit() dunda dolduruluyor.

    // Bu deðiþken tüm client'lara senkronize edilecek.
    // ReadPermission.Everyone -> Herkes okuyabilir
    // WritePermission.Server -> Sadece server deðiþtirebilir
    public NetworkVariable<int> TeamId = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Unity Stuff")]
    public Image healthBar;

    [SerializeField] private float myCost;
    [SerializeField] private float myPrize;
    [SerializeField] private Transform myBarrel;
    [SerializeField] private float myRange;


    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private TargetDetector targetDetector;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable'larýn senkronizasyonu tamamlandýðýnda bu metot çalýþýr.

        // 2. Sadece Server üzerinde çalýþacak mantýk
        if (IsServer)
        {
            myHealth.Value = MaxHealth;

            gameObject.name = TeamId.Value.ToString();

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
        // YALNIZCA SUNUCUDA ÇALIÞTIR!
        if (!IsServer) return;
        // Ölen askerin düþman takým ID'sini bul
        int killingPlayerTeamId = (TeamId.Value == 1) ? 2 : 1;

        // YÖNETÝCÝ ARACILIÐIYLA DÝREKT ERÝÞÝM (O(1))
        PlayerSC enemyPlayerSC = OneVsOneModePlayerSCHolder.Instance.GetPlayerByTeamId(killingPlayerTeamId);

        if (enemyPlayerSC != null)
        {
            enemyPlayerSC.UpdateMyScrap(myPrize);
            Debug.Log($"[Server] Manager: Team {killingPlayerTeamId} gained {myPrize} scrap.");
        }

        GetComponent<NetworkObject>().Despawn();
        //// Ölen askerin düþmanýnýn kim olduðunu bul (Örn: TeamId 1 ise düþman TeamId 2)
        //int killingPlayerTeamId = (TeamId.Value == 1) ? 2 : 1;

        //// Askerin ödülünü PlayerSC'ye gönder (örneðin myPrize kadar scrap ver)
        //GiveScrapToKillingPlayer(killingPlayerTeamId, myPrize);

        ////gameObject.SetActive(false);
        //GetComponent<NetworkObject>().Despawn();
    }

    // Yeni metot: Öldüren oyuncuya Scrap verir
    private void GiveScrapToKillingPlayer(int winningTeamId, float amount)
    {
        // Bütün NetworkObject'lar içinde PlayerSC'leri ara
        foreach (var playerSC in FindObjectsOfType<PlayerSC>())
        {
            if (playerSC.TeamId.Value == winningTeamId)
            {
                // Rakip oyuncu bulundu, Scrap miktarýný Server'da deðiþtir.
                // Bu metot zaten Server'da çalýþtýðý için direkt çaðýrmak güvenlidir.
                playerSC.UpdateMyScrap(amount);
                Debug.Log($"[Server] Team {winningTeamId} player gained {amount} scrap for killing a soldier.");
                return;
            }
        }
        Debug.LogError($"[Server] PlayerSC with TeamId {winningTeamId} not found to give scrap!");
    }

}
