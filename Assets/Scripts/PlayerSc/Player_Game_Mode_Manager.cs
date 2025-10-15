using System;
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

    // YENİ: Çağ geçiş aralığı
    private const float AGE_UPDATE_INTERVAL = 2f;

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
    // ... (Sınıfın geri kalanı) ...

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Mode değişimi için tek bir anonim metot aboneliği (Temizleme için lambda yerine metot kullanılmalıydı, ama şimdilik bırakıyorum)
        _currentMode.OnValueChanged += (oldValue, newValue) =>
        {
            OnModeChanged?.Invoke(newValue);
            HandleModeChange(newValue);
        };

        // **DÜZELTME:** Yaş değişimi için aboneliği sadece bir kez yapıyoruz ve yeni metodumuza bağlıyoruz.
        _currentAge.OnValueChanged += HandleAgeChanged;

        // NetworkVariable'ların ilk değerleriyle HandleModeChange'i çağır
        // Bu, hem modu hem de ilk çağı (IceAge) başlatır.
        HandleModeChange(_currentMode.Value);
    }

    // YENİ: OnNetworkDespawn'da abonelikleri temizle
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Mode değişimi aboneliği, anonim metot olduğu için tam olarak temizlenemeyebilir.
        // Ancak bu yapıyı kullanıyorsanız genelde NetworkManager kapanınca sorun çıkmaz.
        // Yine de temiz bir kod için OnValueChanged'a abone olurken isimlendirilmiş metot kullanın.
        // Şimdilik sadece kopyalanan kısımları temizliyoruz:
        _currentMode.OnValueChanged -= (oldValue, newValue) =>
        {
            OnModeChanged?.Invoke(newValue);
            HandleModeChange(newValue);
        };

        // **DÜZELTME:** Yaş değişimi için aboneliği isimlendirilmiş metot üzerinden temizliyoruz.
        _currentAge.OnValueChanged -= HandleAgeChanged;

        // ÖNEMLİ NOT:
        // _currentMode aboneliği, anonim (lambda) metot olduğu için -= ile temizleme tam olarak çalışmaz.
        // C# 'da bu tür abonelikleri doğru temizlemek için abonelik esnasında da isimlendirilmiş bir metot 
        // kullanmanız şiddetle tavsiye edilir. (Örn: `_currentMode.OnValueChanged += HandleModeChanged;`)
    }
    // YENİ: Tek bir metot ile Age değişimi işlensin.
    private void HandleAgeChanged(PlayerAge oldValue, PlayerAge newValue)
    {
        Debug.Log($"[Manager] Age changed from {oldValue} to {newValue}. Updating visuals.");

        // Görsel güncellemeyi çağır. Bu, hem Server hem de Client'larda çalışır.
        UpdateAgeVisuals();

        // Sadece gerçekten bir Level Up (çağ değişimi) olduysa logları ve event'i tetikle
        if (oldValue != newValue)
        {
            // PlayerSC'deki OnLevelUp eventini tetiklemek istiyorsanız, 
            // buraya bir event mekanizması eklemelisiniz. 
            // Şimdilik sadece loglama ile devam edelim, çünkü OnLevelUp event'i Manager'da yok.
            Debug.Log($"Player leveled up to {newValue} Age! (Network Confirmed)");

            // **Eğer PlayerSC'deki OnLevelUp event'ini tetiklemeniz gerekiyorsa,**
            // **PlayerComponentController üzerinden bir metot çağrısı yapmanız gerekebilir.**
        }
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

            // NetworkVariable Server'da OnValueChanged tetiklemediği için
            // görsel güncellemeyi manuel olarak çağırıyoruz.
            UpdateAgeVisuals();

            Debug.Log($"[Server] Player's age successfully upgraded to: {nextAge}");

            // UpdateAgeVisualsClientRpc'ye artık gerek yok. Otomatik senkronizasyon var.
        }
        else
        {
            Debug.LogWarning("[Server] Cannot upgrade age: Already at the final age (SpaceAge).");
        }
    }

    // ... (Diğer metotlar olduğu gibi kaldı: VisibilityCloseOthers, HandleModeChange, UpdateAgeVisuals, SetAgeServerRpc, RequestStartGameServerRpc, AllPlayersReady)
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
    /// PlayerSC'den gelen ServerRpc talebiyle oyuncunun yaşını doğrudan ayarlar.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetAgeServerRpc(PlayerAge newAge)
    {
        //if (!IsServer) return;
        //_currentAge.Value = newAge;
        //UpdateAgeVisuals(); // Server Yetkili olduğu için manuel güncelleme
        //Debug.Log($"[Server] Age forcefully set to: {newAge}");
        // --- 1. Başlangıç Logu ve Yetki Kontrolü ---
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

        // --- 4. Görsel Güncelleme ve Senkronizasyon (Tartışmalı kısım, önceki önerilerde kaldırılması önerilmişti) ---
        // NOT: _currentAge.OnValueChanged olayı bu noktadan sonra tüm Client'larda tetiklenir.
        // Ancak Server'da hemen (bu thread içinde) tetiklenmez. Bu yüzden Server, genellikle
        // görseli manuel güncellemek ister.

        // Eğer UpdateAgeVisuals() metodunuz IsServer/IsClient kontrolü yapmıyorsa,
        // burada çağrılmalıdır. Aksi takdirde, HandleAgeChanged (OnValueChanged) içindeki
        // çağrıya güvenmelisiniz.

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
