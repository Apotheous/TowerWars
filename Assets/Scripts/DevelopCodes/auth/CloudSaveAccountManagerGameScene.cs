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
        // 2. Kontrol Mekanizmas�:
        if (Instance == null)
        {
            // E�er daha �nce bir �rnek yoksa, bu �rne�i tekil �rnek olarak ayarla.
            Instance = this;

            // (Opsiyonel) Sahneler aras� yok edilmesini engelle
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            // E�er zaten bir �rnek varsa (yani bu ikinci �rnek), kendini yok et.
            Debug.LogWarning("Sahneye ikinci bir GameManager eklendi. Fazlal�k olan yok ediliyor.");
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
            Debug.LogError($"InitializeAsync s�ras�nda hata: {ex.Message}");
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
            // E�er zaten giri� yap�lm��sa tekrar giri� yapma
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Zaten giri� yap�lm��, yeniden giri� yap�lmayacak.");
            }
            else if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("Mevcut oturum bulundu, otomatik giri� yap�l�yor...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            else
            {
                Debug.Log("Kay�tl� oturum yok, kullan�c�dan giri� bilgisi bekleniyor.");
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Giri� ba�ar�l�! ID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...");
                LoadData(); // Kullan�c� verilerini y�kle
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Oturum kontrol� s�ras�nda hata: {ex.Message}");
        }
    }

    public async void LoadData()
    {
        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "AccountName", "PlayerName", "Score" });

            var loadedData = PlayerData.FromDictionary(results);
            Debug.Log($"Y�klendi: {loadedData.AccountName}, Score {loadedData.Score}");
            WriteScore(loadedData.Score);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Y�kleme hatas�: {ex.Message}");
        }
    }

    private void WriteScore(int newScore)
    {
        Debug.Log("Skor yaz�l�yor: " + newScore);
        playerScoreTexter.text = $"Score: {newScore}";
    }
}
