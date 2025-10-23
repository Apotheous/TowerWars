using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class CloudSaveAccountManagerGameScene : MonoBehaviour
{
    public static CloudSaveAccountManagerGameScene Instance;
    //[SerializeField] private TextMeshProUGUI playerScoreTexter;
    [SerializeField] private TextMeshProUGUI playerNameTexter;
    public string myPlayerName;

    private void Awake()
    {
        // 🚫 Önce server kontrolü
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("CloudSaveAccountManagerGameScene serverda devre dışı bırakıldı.");
            enabled = false;
            return;
        }
        
        // ✅ Singleton kontrolü
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("İkinci bir CloudSaveAccountManagerGameScene bulundu, yok ediliyor.");
            Destroy(gameObject);
            return;
        }

        // Client başlatma
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
            Debug.LogError($"InitializeAsync sırasında hata: {ex.Message}");
        }
    }
    // Update is called once per frame

    private async Task CheckForSavedSession()
    {
        try
        {
            // Eğer zaten giriş yapılmışsa tekrar giriş yapma
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Zaten giriş yapılmış, yeniden giriş yapılmayacak.");
            }
            else if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("Mevcut oturum bulundu, otomatik giriş yapılıyor...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            else
            {
                Debug.Log("Kayıtlı oturum yok, kullanıcıdan giriş bilgisi bekleniyor.");
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Giriş başarılı! ID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...");
                LoadData(); // Kullanıcı verilerini yükle
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Oturum kontrolü sırasında hata: {ex.Message}");
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
            myPlayerName = loadedData.PlayerName;
            WriteScore(loadedData.Score);
            WritePlayerNickName(loadedData.PlayerName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Yükleme hatası: {ex.Message}");
        }
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

    private void WriteScore(int newScore)
    {
        Debug.Log("Skor yazılıyor: " + newScore);
        //playerScoreTexter.text = $"Score: {newScore}";
    }

    public async void UpdateScore(int addedScore)
    {
        try
        {
            // Önce mevcut veriyi yükle
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "Score" });

            int currentScore = 0;
            if (results.TryGetValue("Score", out var score))
                currentScore = score.Value.GetAs<int>();

            int newScore = currentScore + addedScore;

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


}
