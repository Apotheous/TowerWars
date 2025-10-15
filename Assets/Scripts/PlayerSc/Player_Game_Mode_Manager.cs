using System;
using Unity.Netcode;
using UnityEngine;

public class Player_Game_Mode_Manager : NetworkBehaviour
{
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
    private NetworkVariable<PlayerAge> _currentAge = new NetworkVariable<PlayerAge>(
        PlayerAge.IceAge,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public PlayerAge CurrentAge => _currentAge.Value; // Dışarıdan okuma için Property

    // Senkronize Mode değişkeni
    private NetworkVariable<PlayerMode> _currentMode = new NetworkVariable<PlayerMode>(PlayerMode.MainMenu);

    public event Action<PlayerMode> OnModeChanged;
    // store handlers so unsubscribe works
    private NetworkVariable<PlayerMode>.OnValueChangedDelegate modeChangedHandler;
    private NetworkVariable<PlayerAge>.OnValueChangedDelegate ageChangedHandler;
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


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // create named delegates (store them)
        modeChangedHandler = OnModeNetworkChanged;
        ageChangedHandler = OnAgeNetworkChanged;

        _currentMode.OnValueChanged += modeChangedHandler;
        _currentAge.OnValueChanged += ageChangedHandler;

        // If this object spawns in OneVsOne already, ensure visuals apply
        // (safeguard for late-joining clients)
        HandleModeChange(_currentMode.Value);
        UpdateAgeVisuals(); // run once so clients reflect current age immediately
    }

    // YENİ: OnNetworkDespawn'da abonelikleri temizle
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // unsubscribe the exact same delegates
        if (modeChangedHandler != null) _currentMode.OnValueChanged -= modeChangedHandler;
        if (ageChangedHandler != null) _currentAge.OnValueChanged -= ageChangedHandler;
    }

    // Named handlers
    private void OnModeNetworkChanged(PlayerMode oldMode, PlayerMode newMode)
    {
        OnModeChanged?.Invoke(newMode);
        HandleModeChange(newMode);
    }

    private void OnAgeNetworkChanged(PlayerAge oldAge, PlayerAge newAge)
    {
        // DEBUG log to confirm client receives the change:
        Debug.Log($"[Client/Manager] OnAgeNetworkChanged: {oldAge} -> {newAge} (IsServer={IsServer}, IsOwner={IsOwner})");

        // Update visuals on client as well
        UpdateAgeVisuals();
    }
    // ... (Diğer metotlar olduğu gibi kaldı: VisibilityCloseOthers, HandleModeChange, UpdateAgeVisuals, SetAgeServerRpc, RequestStartGameServerRpc, AllPlayersReady)
    private void VisibilityCloseOthers(GameObject onAgeGo)
    {
        foreach (var vis in Visibilities)
        {
            if (vis == null) continue;
            if (vis != onAgeGo)
                vis.SetActive(false);
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

    public void SetNewAge(PlayerAge newAge)
    {

        Debug.Log($"[Manager/RPC] SetAgeServerRpc çağrıldı. İstenen Yaş: {newAge}. Mevcut Yaş: {CurrentAge}.");

        if (!IsServer)
        {
            Debug.LogWarning("[Manager/RPC] SetAgeServerRpc çağrısı bir ClientTan geldi, ancak sadece Server yetkilidir. İşlem İPTAL EDİLDİ.");
            return;
        }

        // --- 2. Aynı Yaş Kontrolü (Gereksiz İşlemi Önler) ---
        if (_currentAge.Value == newAge)
        {
            Debug.LogWarning($"[Manager/RPC] Yaş zaten {newAge}! Gereksiz çağrı tespit edildi. İşlem durduruldu.");
            return;
        }

        PlayerAge oldAge = _currentAge.Value; // Değişim öncesi yaşı kaydet

        // --- 3. NetworkVariable Değişimi ---
        Debug.Log($"[Manager/RPC] NetworkVariable _currentAge.Value değiştiriliyor: {oldAge} → {newAge}");
        _currentAge.Value = newAge;

        UpdateAgeVisuals();
        Debug.Log("[Manager/RPC] UpdateAgeVisuals manuel olarak Server'da çağrıldı.");

        // --- 5. Sonuç Logu ---
        Debug.Log($"[Server] Age forcefully set to: {newAge}. RPC başarıyla tamamlandı.");

    }

    /// <summary>
    /// Network üzerinden senkronize edilen CurrentAge değerine göre 
    /// oyuncunun görsel modelini günceller.
    /// </summary>
    private void UpdateAgeVisuals()
    {
        // Yalnızca OneVsOne modunda çalıştır
        if (_currentMode.Value != PlayerMode.OneVsOne) return;

        Debug.Log($"[Manager] UpdateAgeVisuals running on {(IsServer ? "Server" : "Client")} - CurrentAge={_currentAge.Value}");

        // kapat hepsini önce (güvenli)
        foreach (var v in Visibilities) if (v) v.SetActive(false);

        switch (_currentAge.Value)
        {
            case PlayerAge.IceAge:
                if (IceAgeVisibility) IceAgeVisibility.SetActive(true);
                VisibilityCloseOthers(IceAgeVisibility);
                break;
            case PlayerAge.MediavalAge:
                if (MediavalAgeVisibility) MediavalAgeVisibility.SetActive(true);
                VisibilityCloseOthers(MediavalAgeVisibility);
                break;
            case PlayerAge.ModernAge:
                if (ModernAgeVisibility) ModernAgeVisibility.SetActive(true);
                VisibilityCloseOthers(ModernAgeVisibility);
                break;
            case PlayerAge.SpaceAge:
                if (SpaceAgeVisibility) SpaceAgeVisibility.SetActive(true);
                VisibilityCloseOthers(SpaceAgeVisibility);
                break;
        }
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
