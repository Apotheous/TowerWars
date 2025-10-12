using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class Soldier : NetworkBehaviour, IDamageable
{
    public Player_Game_Mode_Manager.PlayerAge age;
    [SerializeField] private float MaxHealth = 100f;



    // Health de�erini a�da senkronize tutuyoruz
    private NetworkVariable<float> myHealth = new NetworkVariable<float>(
        100f,  // varsay�lan de�er
        NetworkVariableReadPermission.Everyone,  // herkes okuyabilir
        NetworkVariableWritePermission.Server    // sadece server yazabilir
    );

    //PlayerProductionManagement ProduceNextUnit() dunda dolduruluyor.

    // Bu de�i�ken t�m client'lara senkronize edilecek.
    // ReadPermission.Everyone -> Herkes okuyabilir
    // WritePermission.Server -> Sadece server de�i�tirebilir
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
        // NetworkVariable'lar�n senkronizasyonu tamamland���nda bu metot �al���r.

        // 2. Sadece Server �zerinde �al��acak mant�k
        if (IsServer)
        {
            myHealth.Value = MaxHealth;

            gameObject.name = TeamId.Value.ToString();

            // �rne�in, burada tak�m�na g�re bir hedef bulma mant��� �al��t�r�labilir.
            // FindInitialTarget(); 


            // 1. TeamId'yi G�VENL� bir �ekilde al ve sakla
            var soldiersControllerNavMesh = GetComponent<SoldiersControllerNavMesh>();
            if (soldiersControllerNavMesh != null)
            {
                // Coroutine'i bu �ekilde ba�latmal�s�n:
                StartCoroutine(soldiersControllerNavMesh.FindTargetAndSetDestination());
            }
            else
            {
                Debug.LogError("Bu objenin �zerinde SoldiersControllerNavMesh component'i bulunamad�!", gameObject);
            }
        }
        // 3. T�m client'larda �al��acak mant�k (G�rsel g�ncellemeler vb.)
        myHealth.OnValueChanged += OnHealthChanged;
        OnHealthChanged(0, myHealth.Value); // UI'� ilk de�erle g�ncelle
        if (targetDetector != null)
        {
            targetDetector.WhenNetworkSpawn();
        }
        else
        {
            Debug.LogError("Bu objenin �zerinde TargetDetector component'i bulunamad�!", gameObject);
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
        TakeDamage(damageAmount); // interface metodunu �a��r
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
        // YALNIZCA SUNUCUDA �ALI�TIR!
        if (!IsServer) return;
        // �len askerin d��man tak�m ID'sini bul
        int killingPlayerTeamId = (TeamId.Value == 1) ? 2 : 1;

        // Y�NET�C� ARACILI�IYLA D�REKT ER���M (O(1))
        PlayerSC enemyPlayerSC = OneVsOneModePlayerSCHolder.Instance.GetPlayerByTeamId(killingPlayerTeamId);

        if (enemyPlayerSC != null)
        {
            enemyPlayerSC.UpdateMyScrap(myPrize);
            Debug.Log($"[Server] Manager: Team {killingPlayerTeamId} gained {myPrize} scrap.");
        }

        GetComponent<NetworkObject>().Despawn();
        //// �len askerin d��man�n�n kim oldu�unu bul (�rn: TeamId 1 ise d��man TeamId 2)
        //int killingPlayerTeamId = (TeamId.Value == 1) ? 2 : 1;

        //// Askerin �d�l�n� PlayerSC'ye g�nder (�rne�in myPrize kadar scrap ver)
        //GiveScrapToKillingPlayer(killingPlayerTeamId, myPrize);

        ////gameObject.SetActive(false);
        //GetComponent<NetworkObject>().Despawn();
    }

    // Yeni metot: �ld�ren oyuncuya Scrap verir
    private void GiveScrapToKillingPlayer(int winningTeamId, float amount)
    {
        // B�t�n NetworkObject'lar i�inde PlayerSC'leri ara
        foreach (var playerSC in FindObjectsOfType<PlayerSC>())
        {
            if (playerSC.TeamId.Value == winningTeamId)
            {
                // Rakip oyuncu bulundu, Scrap miktar�n� Server'da de�i�tir.
                // Bu metot zaten Server'da �al��t��� i�in direkt �a��rmak g�venlidir.
                playerSC.UpdateMyScrap(amount);
                Debug.Log($"[Server] Team {winningTeamId} player gained {amount} scrap for killing a soldier.");
                return;
            }
        }
        Debug.LogError($"[Server] PlayerSC with TeamId {winningTeamId} not found to give scrap!");
    }

}
