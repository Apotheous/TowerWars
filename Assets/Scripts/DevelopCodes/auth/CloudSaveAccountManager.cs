using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class CloudSaveAccountManager : MonoBehaviour
{
    // 1. Statik referans: Dışarıdan erişim için
    public static CloudSaveAccountManager Instance { get; private set; }
    // UI alanları
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI AccountNameText;
    [SerializeField] private TextMeshProUGUI ScoreTxt;
    [SerializeField] private Button createAccountBtnTmp;
    [SerializeField] private GameObject accounManagementPanel;


    // Geleneksel giriş için yeni alanlar (Inspector'dan UI InputField'lere bağlayın)
    [SerializeField] private TMP_InputField accountNameInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button signInButton; // Yeni bir Giriş butonu ekleyin
    [SerializeField] private Button ScroeUpBtn; // score denemek için 
    [SerializeField] private Button signOutBtn; // Yeni bir Giriş butonu ekleyin

    [SerializeField] private TextMeshProUGUI playerNameTexter;
    [SerializeField] private TextMeshProUGUI playerScoreTexter;

    private void OnValidate()
    {
        // Kod çalışmadan önce, Inspector'da UI bileşenlerini kontrol etmenizi sağlar.
        if (createAccountBtnTmp == null) Debug.LogError("createAccountBtnTmp is not assigned.");
        if (accountNameInput == null) Debug.LogError("usernameInput is not assigned.");
        if (passwordInput == null) Debug.LogError("passwordInput is not assigned.");
        if (signInButton == null) Debug.LogError("signInButton is not assigned. Consider adding a separate SignIn button.");
    }

    private void Awake()
    {
        // 2. Kontrol Mekanizması:
        if (Instance == null)
        {
            // Eğer daha önce bir örnek yoksa, bu örneği tekil örnek olarak ayarla.
            Instance = this;

            // (Opsiyonel) Sahneler arası yok edilmesini engelle
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Eğer zaten bir örnek varsa (yani bu ikinci örnek), kendini yok et.
            Debug.LogWarning("Sahneye ikinci bir GameManager eklendi. Fazlalık olan yok ediliyor.");
            Destroy(gameObject);
        }
        _ = InitializeAsync();
    }
    // InitializeAsync: UnityServices.InitializeAsync ve oturum kontrolünü yapar
    private async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("UGS Services Initialized.");

            await CheckForSavedSession();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"InitializeAsync sırasında hata: {ex.Message}");
        }
    }

    private void Start()
    {
        createAccountBtnTmp.onClick.AddListener(SignUp);
        signInButton.onClick.AddListener(SignIn);
        ScroeUpBtn.onClick.AddListener(ScoreUp);
        signOutBtn.onClick.AddListener(SignOut);
    }

    //-------------------------------------------------------------
    // 1. KULLANICI OLUŞTURMA (Sign Up)
    //-------------------------------------------------------------
    public async void SignUp()
    {
        string username = accountNameInput.text;
        string password = passwordInput.text;

        try
        {
            // Yeni bir kullanıcı hesabı oluştur
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            // Başarılı olursa, zaten giriş yapmış (Signed In) demektir
            Debug.Log($"Kayıt Başarılı! Player ID: {AuthenticationService.Instance.PlayerId}");

            // Kullanıcı adı ve ID'sini ekrana yaz
            AccountNameText.text = $"Giriş: {username}\nID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";

            // İlk kayıttan sonra veri kaydetme işlemini başlat (isteğe bağlı)
            SaveData();
        }
        catch (AuthenticationException ex)
        {
            // Örneğin: "Kullanıcı adı zaten kullanımda" veya "Şifre/Kullanıcı adı çok kısa" hatalarını yakala
            Debug.LogError($"Kayıt Hatası: {ex.Message}");
            AccountNameText.text = "Kayıt Hatası: " + ex.Message.Split('\n')[0];
        }
    }

    //-------------------------------------------------------------
    // 2. GİRİŞ YAPMA (Sign In)
    //-------------------------------------------------------------
    public async void SignIn()
    {
        string username = accountNameInput.text;
        string password = passwordInput.text;

        try
        {
            // Mevcut bir kullanıcı hesabı ile giriş yap
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            Debug.Log($"Giriş Başarılı! Player ID: {AuthenticationService.Instance.PlayerId}");

            // Giriş yaptıktan sonra mutlaka verileri yüklemeyi deneyin
            LoadData();

            AccountNameText.text = $"Giriş: {username}\nID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";
        }
        catch (AuthenticationException ex)
        {
            // Örneğin: "Yanlış kullanıcı adı veya şifre"
            Debug.LogError($"Giriş Hatası: {ex.Message}");
            AccountNameText.text = "Giriş Hatası: " + ex.Message.Split('\n')[0];
        }
    }

    //-------------------------------------------------------------
    // 3. CLOUD SAVE İŞLEMLERİ (Giriş yaptıktan sonra kullanılır)
    //-------------------------------------------------------------

    public async void SaveData()
    {
        var playerData = new PlayerData
        {
            AccountName = accountNameInput.text,
            PlayerName = playerNameInput.text,
            Score = 0,
        };

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(playerData.ToDictionary());
            Debug.Log("Veri başarıyla kaydedildi!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Kaydetme hatası: {ex.Message}");
        }
    }

    public async void LoadData()
    {
        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "PlayerName", "Level", "Score", "Coins", "LastLogin" });

            var loadedData = PlayerData.FromDictionary(results);
            Debug.Log($"Yüklendi: {loadedData.AccountName}, Score {loadedData.Score}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Yükleme hatası: {ex.Message}");
        }
    }


    // 🔹 1. Mevcut oturumu kontrol et
    private async Task CheckForSavedSession()
    {
        try
        {
            // Session token hâlâ geçerli mi?
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("Mevcut oturum bulundu, otomatik giriş yapılıyor...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Otomatik yeniden bağlanma
            }
            else
            {
                Debug.Log("Kayıtlı oturum yok, kullanıcıdan giriş bilgisi bekleniyor.Lütfen hesap açın");
                if (!accounManagementPanel.activeSelf)
                {
                    accounManagementPanel.SetActive(true);
                }
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AccountNameText.text = $"Otomatik giriş başarılı! ID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";
                if (accounManagementPanel.activeSelf)
                {
                    accounManagementPanel.SetActive(false);
                    await LoadPlayerNameFromCloud();
                    await LoadPlayerScoreFromCloud();
                }
                LoadData(); // Kullanıcı verilerini yükle
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Oturum kontrolü sırasında hata: {ex.Message}");
        }
    }

    // 🔹 7. Oturumdan çıkış (isteğe bağlı)
    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        Debug.Log("Kullanıcı çıkış yaptı.");
        AccountNameText.text = "Çıkış yapıldı.";
    }
    public void ScoreUp()
    {
        UpdateScore(5);

        
    }
    public async void UpdateScore(float addedScore)
    {
        try
        {
            // Önce mevcut veriyi yükle
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "Score" });

            float currentScore = 0;
            if (results.TryGetValue("Score", out var score))
                currentScore = score.Value.GetAs<int>();

            float newScore = currentScore + addedScore;

            // Yeni skoru kaydet
            await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, object> { { "Score", newScore } });

            Debug.Log($"Skor güncellendi! Yeni skor: {newScore}");
            //WriteScore(newScore);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Skor güncelleme hatası: {ex.Message}");
        }
    }
    private void WriteScore(float newScore)
    {
        playerScoreTexter.text = $"Score: {newScore}";
    }
    private void WritePlayerNickName(string playerName)
    {
        playerNameTexter.text = playerName;
    }

    // 🔹 Cloud Save'den sadece PlayerName verisini çeker
    public async Task LoadPlayerNameFromCloud()
    {
        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "PlayerName" });

            if (results.TryGetValue("PlayerName", out var playerNameItem))
            {
                string playerName = playerNameItem.Value.GetAs<string>();
                Debug.Log($"Cloud'dan çekilen oyuncu ismi: {playerName}");
                WritePlayerNickName(playerName);
            }
            else
            {
                Debug.LogWarning("Cloud Save'de 'PlayerName' verisi bulunamadı.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PlayerName yükleme hatası: {ex.Message}");
        }
    }
    // 🔹 Cloud Save'den sadece Score verisini çeker
    public async Task LoadPlayerScoreFromCloud()
    {
        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "Score" });

            if (results.TryGetValue("Score", out var scoreItem))
            {
                float playerScore = scoreItem.Value.GetAs<int>();
                Debug.Log($"Cloud'dan çekilen skor: {playerScore}");
                WriteScore(playerScore);
            }
            else
            {
                Debug.LogWarning("Cloud Save'de 'Score' verisi bulunamadı.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Score yükleme hatası: {ex.Message}");
        }
    }
}
