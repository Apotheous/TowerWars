using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_Game_Mode_Manager : NetworkBehaviour
{
    // YENİ: Yaş sırasını yöneten sabit bir dizi
    private static readonly PlayerAge[] AgeProgression =
    {
        PlayerAge.IceAge,
        PlayerAge.MediavalAge,
        PlayerAge.ModernAge,
        PlayerAge.SpaceAge
    };

    // --- ENUMLAR ---
    public enum PlayerMode
    {
        MainMenu,
        OneVsOne
    }

    public enum PlayerAge
    {
        IceAge,
        MediavalAge,
        ModernAge,
        SpaceAge
    }

    // --- SERİLEŞTİRİLMİŞ ALANLAR ---
    [SerializeField] private PlayerComponentController playerComponentController;
    [SerializeField] private GameObject myCam;
    [SerializeField] private GameObject oneVSOneMode;
    [SerializeField] private GameObject[] Visibilities;

    [SerializeField] private GameObject IceAgeVisibility;
    [SerializeField] private GameObject MediavalAgeVisibility;
    [SerializeField] private GameObject ModernAgeVisibility;
    [SerializeField] private GameObject SpaceAgeVisibility;

    // --- NETWORK VARIABLES ---
    // [SerializeField] public PlayerAge age; // ESKİ YEREL DEĞİŞKEN SİLİNDİ

    // YENİ: Yaş NetworkVariable olarak tanımlandı
    private NetworkVariable<PlayerAge> _currentAge = new NetworkVariable<PlayerAge>(
        PlayerAge.IceAge,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public PlayerAge CurrentAge => _currentAge.Value; // Dışarıdan okuma için Property

    // Senkronize Mode değişkeni
    private NetworkVariable<PlayerMode> _currentMode = new NetworkVariable<PlayerMode>(PlayerMode.MainMenu);

    public event Action<PlayerMode> OnModeChanged;

    public PlayerMode CurrentMode
    {
        get => _currentMode.Value;
        private set
        {
            if (_currentMode.Value != value)
            {
                _currentMode.Value = value;
                OnModeChanged?.Invoke(value);
                // HandleModeChange otomatik olarak NetworkVariable OnValueChanged ile tetiklenecek.
            }
        }
    }

    // YENİ: Start() mantığı OnNetworkSpawn'a taşındı.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Mode değişimi için abonelik
        _currentMode.OnValueChanged += (oldValue, newValue) =>
        {
            OnModeChanged?.Invoke(newValue);
            HandleModeChange(newValue);
        };

        // YENİ: Yaş değişimi için abonelik
        _currentAge.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"[Manager] Age changed from {oldValue} to {newValue}. Updating visuals.");
            // Yaş değiştiğinde sadece görsel güncellemeyi çağır.
            UpdateAgeVisuals();
        };

        // NetworkVariable'ların ilk değerleriyle HandleModeChange'i çağır
        // Bu, hem modu hem de ilk çağı (IceAge) başlatır.
        HandleModeChange(_currentMode.Value);

        if (IsServer)
        {
            _currentAge.Value = PlayerAge.IceAge;
        }
    }

    // YENİ: OnNetworkDespawn'da abonelikleri temizle
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        _currentMode.OnValueChanged -= (oldValue, newValue) =>
        {
            OnModeChanged?.Invoke(newValue);
            HandleModeChange(newValue);
        };

        _currentAge.OnValueChanged -= (oldValue, newValue) =>
        {
            HandleModeChange(CurrentMode);
        };
    }

    /// <summary>
    /// Server'da oyuncunun yaşını bir sonraki seviyeye yükseltir.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UpgradeAgeServerRpc()
    {
        if (!IsServer) return;

        // Mevcut yaşın sırasını bul (Artık CurrentAge property'si kullanılıyor)
        int currentAgeIndex = Array.IndexOf(AgeProgression, CurrentAge);

        if (currentAgeIndex >= 0 && currentAgeIndex < AgeProgression.Length - 1)
        {
            PlayerAge nextAge = AgeProgression[currentAgeIndex + 1];

            // Yaşı NetworkVariable üzerinden değiştir. Bu, Client'larda OnValueChanged'ı tetikler.
            _currentAge.Value = nextAge;
            UpdateAgeVisuals();
            Debug.Log($"[Server] Player's age successfully upgraded to: {nextAge}");

            // UpdateAgeVisualsClientRpc'ye artık gerek yok. Otomatik senkronizasyon var.
        }
        else
        {
            Debug.LogWarning("[Server] Cannot upgrade age: Already at the final age (SpaceAge).");
        }
    }

    private void VisibilityCloseOthers(GameObject onAgeGo)
    {
        foreach (var vis in Visibilities)
        {
            // Dikkat: Burada onAgeGo.name ile vis.name'i karşılaştırmak, 
            // objelerin Editor'da doğru adlandırılmasına bağlıdır.
            if (onAgeGo.name != vis.name)
            {
                if (vis) vis.SetActive(false);
            }

        }
    }
    
    // Public yapıldı, böylece _currentMode.OnValueChanged eventi onu çağırabilir.
    public void HandleModeChange(PlayerMode newMode)
    {
        switch (newMode)
        {
            case PlayerMode.MainMenu:
                if (oneVSOneMode) oneVSOneMode.SetActive(false);
                playerComponentController.SetComponentsActive(false);
                // MainMenu'ye geçince tüm görselleri de kapatabiliriz (isteğe bağlı)
                // VisibilityCloseOthers(null); 
                break;

            case PlayerMode.OneVsOne:
                if (oneVSOneMode) oneVSOneMode.SetActive(true);
                playerComponentController.SetComponentsActive(true);

                // Sorumluluğu yeni metoda devret
                UpdateAgeVisuals();

                // Kamera mantığı yerinde kalır
                if (IsOwner)
                {
                    myCam.SetActive(true);
                }
                else
                {
                    myCam.SetActive(false);
                }

                break;
        }
    }

    /// <summary>
    /// Network üzerinden senkronize edilen CurrentAge değerine göre 
    /// oyuncunun görsel modelini günceller.
    /// </summary>
    private void UpdateAgeVisuals()
    {
        // Yalnızca OneVsOne modunda aktif/pasif etme işlemini yap
        if (CurrentMode != PlayerMode.OneVsOne) return;

        // GÜNCEL: CurrentAge kullanılır.
        if (CurrentAge == PlayerAge.IceAge)
        {
            IceAgeVisibility.SetActive(true);
            Debug.Log("IceAgeVisibility Set Active true");
            VisibilityCloseOthers(IceAgeVisibility);
        }
        else if (CurrentAge == PlayerAge.MediavalAge)
        {
            MediavalAgeVisibility.SetActive(true);
            Debug.Log("MediavalAgeVisibility Set Active true");
            VisibilityCloseOthers(MediavalAgeVisibility);
        }
        else if (CurrentAge == PlayerAge.ModernAge)
        {
            ModernAgeVisibility.SetActive(true);
            Debug.Log("ModernAgeVisibility Set Active true");
            VisibilityCloseOthers(ModernAgeVisibility);
        }
        else if (CurrentAge == PlayerAge.SpaceAge)
        {
            SpaceAgeVisibility.SetActive(true);
            Debug.Log("SpaceAgeVisibility Set Active true");
            VisibilityCloseOthers(SpaceAgeVisibility);
        }
    }
    /// <summary>
    /// PlayerSC'den gelen ServerRpc talebiyle oyuncunun yaşını doğrudan ayarlar.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetAgeServerRpc(PlayerAge newAge)
    {
        if (!IsServer) return;

        // Yaşı NetworkVariable üzerinden değiştir. Bu, Client'larda OnValueChanged'ı tetikler.
        _currentAge.Value = newAge;
        UpdateAgeVisuals();
        Debug.Log($"[Server] Age forcefully set to: {newAge}");
        
        // HandleAgeChangeLevelUp metodu SİLİNDİ.
    }
    // ✅ Client sadece "başlamak istiyorum" diye talepte bulunur
    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Player {senderId} requested to start game.");

        // Burada sunucu kontrol eder → örn: herkes hazır mı?
        if (AllPlayersReady())
        {
            Debug.Log("All players ready, starting game!");
            CurrentMode = PlayerMode.OneVsOne;
        }
        else
        {
            Debug.Log("Start request denied: not all players are ready.");
        }
    }

    // ✅ Dummy kontrol, sonra kendi mantığını koyabilirsin
    private bool AllPlayersReady()
    {
        // Şimdilik sadece "en az 2 oyuncu var mı" diye bakalım
        return NetworkManager.Singleton.ConnectedClients.Count >= 2;
    }

}
