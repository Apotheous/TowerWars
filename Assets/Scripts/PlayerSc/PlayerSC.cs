using System;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class PlayerSC : NetworkBehaviour ,IDamageable
{
    // Bu değişken tüm client'lara senkronize edilecek.
    // ReadPermission.Everyone -> Herkes okuyabilir
    // WritePermission.Server -> Sadece server değiştirebilir
    public NetworkVariable<int> TeamId = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    
    #region Player Data
    [Header("Player Stats")]
    private readonly NetworkVariable<float> initalHealth = new NetworkVariable<float>(1000);

    private readonly NetworkVariable<float> mycurrentHealth = new NetworkVariable<float>();

    private readonly NetworkVariable<float> myExpPoint = new NetworkVariable<float>(0);

    private readonly NetworkVariable<float> myCurrentScrap = new NetworkVariable<float>(999999);
    public NetworkVariable<int> myTempPoint = new NetworkVariable<int>(0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    // Custom eventler
    public event Action<float, float> OnScrapChanged;

    public event Action OnLevelUp;

    private const float LEVEL_UP_IceAge = 0f;
    private const float LEVEL_UP_MediavelAge = 4000f;
    private const float LEVEL_UP_ModernAge = 14000f;
    private const float LEVEL_UP_SpaceAge = 80000f;


    #endregion

    public NetworkVariable<ulong> WinnerClientId = new NetworkVariable<ulong>(0);

    private Player_Game_Mode_Manager player_Game_Mode_Manager;

    public override void OnNetworkSpawn()
    {

        mycurrentHealth.Value = initalHealth.Value;
        if (player_Game_Mode_Manager == null &&IsServer)
        {
            player_Game_Mode_Manager = gameObject.GetComponent<Player_Game_Mode_Manager>();
            Debug.Log("Serverda player_Game_Mode_Manager doldu -- " + player_Game_Mode_Manager.gameObject.name);
        }
       
        //Stat abonelikleri
        mycurrentHealth.OnValueChanged += OnHealthChanged;
        myCurrentScrap.OnValueChanged += OnMyScrapChanged;

        myExpPoint.OnValueChanged += OnExpPointChanged;
        //myExpPoint.OnValueChanged += HandleTechPointChanged;
        
        WinnerClientId.OnValueChanged += OnWinnerDeclared;
        //OnLevelUp += HandleLevelUpServerAction;
        // Debug için event subscribe (isteğe bağlı)

        OnLevelUp += () =>
        {
            Debug.Log("[PlayerSC] LEVEL UP TRIGGERED!");
        };

        // SUNUCU TARAFINDA BAŞLATMA (TeamId ve İlk Değerler)
        if (IsServer)
        {
            // YENİ MANTIK: ClientId 1 -> Team 1; ClientId 2 -> Team 2
            if (OwnerClientId == 1)
            {
                TeamId.Value = 1;
            }
            else if (OwnerClientId == 2)
            {
                TeamId.Value = 2;
            }

            // Log'a yazdır (Server'da)
            Debug.Log($"[Server] Player {OwnerClientId} spawned and assigned to TeamId: {TeamId.Value}");

            // Client'lara senkronize edilen objenin adını sunucuda değiştirmek
            // localPlayer.gameObject.name = "Player_" + NetworkManager.Singleton.LocalClientId;
            // yerine Sunucudaki NetworkObject'un adını değiştiririz.
            gameObject.name = "Player_" + OwnerClientId;

            // TeamId'nin burada ayarlandığından emin olun (Örn: TeamId.Value = 1;)
            if (OneVsOneModePlayerSCHolder.Instance != null)
            {
                OneVsOneModePlayerSCHolder.Instance.RegisterPlayer(TeamId.Value, this);
            }
        }

        // CLIENT TARAFINDA GÖRSEL BAŞLATMA (Opsiyonel)
        if (IsOwner)
        {
            // Bu oyuncunun adını kendi client'ında göster
            gameObject.name = "MY_PLAYER_" + OwnerClientId;
        }


    }


      // YENİ: Ağdan kaldırılırken abonelikleri sonlandırma
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        

        // === ABONELİKLERİN SONLANDIRILMASI ===
        // Obje yok edilmeden önce NetworkVariable olay listesinden kendini çıkarır.
        mycurrentHealth.OnValueChanged -= OnHealthChanged;
        myCurrentScrap.OnValueChanged -= OnMyScrapChanged;
        myExpPoint.OnValueChanged -= OnExpPointChanged;
        //myExpPoint.OnValueChanged -= HandleTechPointChanged;
        WinnerClientId.OnValueChanged -= OnWinnerDeclared;
        //OnLevelUp -= HandleLevelUpServerAction;
        Debug.Log($"[PlayerSC-{OwnerClientId}] Abonelikler sonlandırıldı ve Network Despawn oldu.");
    }


    private void Update()
    {
        Debug.Log("My Temp Point Değeri : " + myTempPoint.Value);
    }
    private void OnWinnerDeclared(ulong previousValue, ulong newValue)
    {
        if (newValue != 0) // 0 = kimse kazanmadı varsayalım
        {
            if (IsOwner) // sadece kendi ekranımda göster
            {
                bool benKazandim = (newValue == NetworkManager.Singleton.LocalClientId);
                OneVsOneGameSceneUISingleton.Instance.ShowGameOver("Oyun bitti!", benKazandim);
            }
        }
    }

    #region Player Stats abonelikler
    private void OnHealthChanged(float previous, float current)
    {
        if (IsOwner)
        {
            OneVsOneGameSceneUISingleton.Instance.PlayerCurrentHealthWrite(current);
        }
    }
    private void OnMyScrapChanged(float previous, float current)
    {
        if (IsOwner)
        {
            OneVsOneGameSceneUISingleton.Instance.PlayerScrapWrite(current);
        }
    }
    private void OnExpPointChanged(float previous, float current)
    {
        if (IsServer)
        {
            HandleTechPointChanged(previous, current);
            Debug.Log($"[OnExpPointChanged]Isowner - Player Current Age = " + player_Game_Mode_Manager.CurrentAge + " oldExpVal =" + previous + "NewExpVal = " + current);
        }
        // UI Güncellemesini sadece Owner Client yapsın
        if (IsOwner)
        {
            OneVsOneGameSceneUISingleton.Instance.PlayerExpWrite(current);
        }
    }

    #endregion



    #region Health Logic

    /// <summary>
    /// Playerın currentHealth değerini değiştirir
    /// </summary>
    /// <param name="damage"></param>
    public void UpdateMyCurrentHealth(float damage)
    {
        if (IsServer)
        {
            mycurrentHealth.Value -= damage;
            if (mycurrentHealth.Value <= 0)
            {
                Die();
            }
        }
        else
        {
            RequestUpdateHealthServerRpc(damage);
        }
    }

    [ServerRpc]
    private void RequestUpdateHealthServerRpc(float damage)
    {
        mycurrentHealth.Value -= damage;
    }


    public void TakeDamage(float damageAmount)
    {
        UpdateMyCurrentHealth(damageAmount);
    }

    public void Die()
    {
        if (IsServer)
        {
            Debug.Log($"Player {OwnerClientId} died (server).");
            OneVsOneGameManager.Instance.HandlePlayerDeath(OwnerClientId);
            // opsiyonel: oyuncu objesini devre dışı bırak
            RpcDisablePlayerVisualsClientRpc();
        }
    }

    [ClientRpc]
    private void RpcDisablePlayerVisualsClientRpc()
    {
        // görselleri/sesleri kapat; kontrol scriptlerini disable et
        var inputComp = GetComponent<PlayerSC>(); // senin input component'ine göre değiştir
        if (inputComp != null && IsOwner) inputComp.enabled = false;
        // animasyon, collider vb. kapat
        // gameObject.SetActive(false); -> dikkat, NetObject de unmanaged olabilir
    }
    #endregion


    #region Scrap Logic
    public float GetMyCurrentScrap()
    {
        return myCurrentScrap.Value;
    }


    /// <summary>
    /// Playerın scrap değerini değiştirir
    /// </summary>
    /// <param name="damage"></param>
    public void UpdateMyScrap(float amount)
    {

        if (IsServer)
        {
            myCurrentScrap.Value += amount;
        }
        else
        {
            RequestUpdateScrapServerRpc(amount);
        }
    }
    public void AddTempPoint()
    {
        if (IsServer)
        {
            myTempPoint.Value += 1;
        }

    }
    [ServerRpc]
    private void RequestUpdateScrapServerRpc(float amount)
    {
        myCurrentScrap.Value += amount;

    }
    #endregion


    #region TechPoint Logic
    private void HandleTechPointChanged(float oldValue, float newValue)
    {
        if (IsServer)
        {
            if (player_Game_Mode_Manager != null)
            {
                Debug.Log(" HandleTechPointChanged = playerCurrent Age = " + player_Game_Mode_Manager.CurrentAge + " oldExpVal --" + oldValue + " -- NewExpVal -- " + newValue + " -- Player ID = --"+TeamId.Value+"--");
                // Yaş yükseltme işlemini Server'da yetkili olarak yönet
                if (player_Game_Mode_Manager.CurrentAge == Player_Game_Mode_Manager.PlayerAge.IceAge && newValue >= LEVEL_UP_MediavelAge)
                {
                    Debug.Log("[PlayerSC] RPC CALL: SetAgeServerRpc MediavalAge."+ " --Player ID = --"+TeamId.Value+"--" ); 
                    player_Game_Mode_Manager.SetNewAge(Player_Game_Mode_Manager.PlayerAge.MediavalAge);
                    Debug.Log(" [PlayerSC] RPC CALL:  = playerCurrent Age = " + player_Game_Mode_Manager.CurrentAge + " oldExpVal --" + oldValue + " -- NewExpVal -- " + newValue + " -- Player ID = --" + TeamId.Value + "--");
                }
                else if (player_Game_Mode_Manager.CurrentAge == Player_Game_Mode_Manager.PlayerAge.MediavalAge && newValue >= LEVEL_UP_ModernAge)
                {
                    Debug.Log("[PlayerSC] RPC CALL: SetAgeServerRpc ModernAge." + " --Player ID = --" + TeamId.Value + "--");
                    player_Game_Mode_Manager.SetNewAge(Player_Game_Mode_Manager.PlayerAge.ModernAge);
                    Debug.Log(" [PlayerSC] RPC CALL:  = playerCurrent Age = " + player_Game_Mode_Manager.CurrentAge + " oldExpVal --" + oldValue + " -- NewExpVal -- " + newValue + " -- Player ID = --" + TeamId.Value + "--");
                }
                else if (player_Game_Mode_Manager.CurrentAge == Player_Game_Mode_Manager.PlayerAge.ModernAge && newValue >= LEVEL_UP_SpaceAge)
                {
                    player_Game_Mode_Manager.SetNewAge(Player_Game_Mode_Manager.PlayerAge.SpaceAge);
                    Debug.Log(" [PlayerSC] RPC CALL:  = playerCurrent Age = " + player_Game_Mode_Manager.CurrentAge + " oldExpVal --" + oldValue + " -- NewExpVal -- " + newValue + " -- Player ID = --" + TeamId.Value + "--");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerSC] player_Game_Mode_Manager null referans hatası!");
            }
        }
    }


    public void UpdateExpPointIncrease(float amount)
    {
        if (IsServer)
        {
            // Server authoritative → direkt değiştir
            myExpPoint.Value += amount;
        }
        else
        {
            // Client → ServerRpc üzerinden artır
            RequestExpPointsServerRpc(amount);
        }
    }


    [ServerRpc]
    private void RequestExpPointsServerRpc(float amount)
    {
        myExpPoint.Value += amount;
    }

  


    #endregion
}
