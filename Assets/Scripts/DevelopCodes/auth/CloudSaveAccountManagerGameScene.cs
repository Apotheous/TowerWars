using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class CloudSaveAccountManagerGameScene : MonoBehaviour
{
    public static CloudSaveAccountManagerGameScene Instance;
    [SerializeField] private TextMeshProUGUI playerScoreTexter;
    private void Awake()
    {
        // 2. Kontrol Mekanizmasý:
        if (Instance == null)
        {
            // Eðer daha önce bir örnek yoksa, bu örneði tekil örnek olarak ayarla.
            Instance = this;

            // (Opsiyonel) Sahneler arasý yok edilmesini engelle
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Eðer zaten bir örnek varsa (yani bu ikinci örnek), kendini yok et.
            Debug.LogWarning("Sahneye ikinci bir GameManager eklendi. Fazlalýk olan yok ediliyor.");
            Destroy(gameObject);
        }
        _ = InitializeAsync();
    }
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
            Debug.LogError($"InitializeAsync sýrasýnda hata: {ex.Message}");
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    private async Task CheckForSavedSession()
    {
        try
        {
            // Eðer zaten giriþ yapýlmýþsa tekrar giriþ yapma
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Zaten giriþ yapýlmýþ, yeniden giriþ yapýlmayacak.");
            }
            else if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("Mevcut oturum bulundu, otomatik giriþ yapýlýyor...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            else
            {
                Debug.Log("Kayýtlý oturum yok, kullanýcýdan giriþ bilgisi bekleniyor.");
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Giriþ baþarýlý! ID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...");
                LoadData(); // Kullanýcý verilerini yükle
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Oturum kontrolü sýrasýnda hata: {ex.Message}");
        }
    }

    public async void LoadData()
    {
        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "AccountName", "PlayerName", "Score" });

            var loadedData = PlayerData.FromDictionary(results);
            Debug.Log($"Yüklendi: {loadedData.AccountName}, Score {loadedData.Score}");
            WriteScore(loadedData.Score);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Yükleme hatasý: {ex.Message}");
        }
    }

    private void WriteScore(int newScore)
    {
        Debug.Log("Skor yazýlýyor: " + newScore);
        playerScoreTexter.text = $"Score: {newScore}";
    }
}
