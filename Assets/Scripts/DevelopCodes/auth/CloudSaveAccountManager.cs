using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class CloudSaveAccountManager : MonoBehaviour
{
    // UI alanlar�
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI AccountNameText;
    [SerializeField] private Button createAccountBtnTmp;

    // Geleneksel giri� i�in yeni alanlar (Inspector'dan UI InputField'lere ba�lay�n)
    [SerializeField] private TMP_InputField accountNameInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button signInButton; // Yeni bir Giri� butonu ekleyin

    private void OnValidate()
    {
        // Kod �al��madan �nce, Inspector'da UI bile�enlerini kontrol etmenizi sa�lar.
        if (createAccountBtnTmp == null) Debug.LogError("createAccountBtnTmp is not assigned.");
        if (accountNameInput == null) Debug.LogError("usernameInput is not assigned.");
        if (passwordInput == null) Debug.LogError("passwordInput is not assigned.");
        if (signInButton == null) Debug.LogError("signInButton is not assigned. Consider adding a separate SignIn button.");
    }

    private async void Awake()
    {
        await UnityServices.InitializeAsync();

        // Bu noktada anonim giri� yapmayaca��z, oyuncunun tercihini bekleyece�iz.
        // Hata ay�klama ama�l�, servislerin ba�lat�ld���n� onaylayal�m.
        Debug.Log("UGS Services Initialized.");
    }

    private void Start()
    {
        // Butonlar� ilgili metotlara ba�la
        // createAccountBtnTmp: KULLANICI OLU�TURMA (Sign Up) olarak kullan�ls�n
        createAccountBtnTmp.onClick.AddListener(SignUp);

        // signInButton: G�R�� YAPMA (Sign In) olarak kullan�ls�n
        signInButton.onClick.AddListener(SignIn);
    }

    //-------------------------------------------------------------
    // 1. KULLANICI OLU�TURMA (Sign Up)
    //-------------------------------------------------------------
    public async void SignUp()
    {
        string username = accountNameInput.text;
        string password = passwordInput.text;

        try
        {
            // Yeni bir kullan�c� hesab� olu�tur
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            // Ba�ar�l� olursa, zaten giri� yapm�� (Signed In) demektir
            Debug.Log($"Kay�t Ba�ar�l�! Player ID: {AuthenticationService.Instance.PlayerId}");

            // Kullan�c� ad� ve ID'sini ekrana yaz
            AccountNameText.text = $"Giri�: {username}\nID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";

            // �lk kay�ttan sonra veri kaydetme i�lemini ba�lat (iste�e ba�l�)
            SaveData();
        }
        catch (AuthenticationException ex)
        {
            // �rne�in: "Kullan�c� ad� zaten kullan�mda" veya "�ifre/Kullan�c� ad� �ok k�sa" hatalar�n� yakala
            Debug.LogError($"Kay�t Hatas�: {ex.Message}");
            AccountNameText.text = "Kay�t Hatas�: " + ex.Message.Split('\n')[0];
        }
    }

    //-------------------------------------------------------------
    // 2. G�R�� YAPMA (Sign In)
    //-------------------------------------------------------------
    public async void SignIn()
    {
        string username = accountNameInput.text;
        string password = passwordInput.text;

        try
        {
            // Mevcut bir kullan�c� hesab� ile giri� yap
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            Debug.Log($"Giri� Ba�ar�l�! Player ID: {AuthenticationService.Instance.PlayerId}");

            // Giri� yapt�ktan sonra mutlaka verileri y�klemeyi deneyin
            LoadData();

            AccountNameText.text = $"Giri�: {username}\nID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";
        }
        catch (AuthenticationException ex)
        {
            // �rne�in: "Yanl�� kullan�c� ad� veya �ifre"
            Debug.LogError($"Giri� Hatas�: {ex.Message}");
            AccountNameText.text = "Giri� Hatas�: " + ex.Message.Split('\n')[0];
        }
    }

    //-------------------------------------------------------------
    // 3. CLOUD SAVE ��LEMLER� (Giri� yapt�ktan sonra kullan�l�r)
    //-------------------------------------------------------------

    public async void SaveData()
    {
        var playerData = new PlayerData
        {
            AccountName = accountNameInput.text,
            PlayerName = playerNameInput.text,
            //Level = 1,
            Score = 0,
            //Coins = 0,
            //LastLogin = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(playerData.ToDictionary());
            Debug.Log("Veri ba�ar�yla kaydedildi!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Kaydetme hatas�: {ex.Message}");
        }
    }

    public async void LoadData()
    {
        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "PlayerName", "Level", "Score", "Coins", "LastLogin" });

            var loadedData = PlayerData.FromDictionary(results);
            Debug.Log($"Y�klendi: {loadedData.AccountName}, Score {loadedData.Score}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Y�kleme hatas�: {ex.Message}");
        }
    }
}
