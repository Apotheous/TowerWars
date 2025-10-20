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
    // UI alanlarý
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI AccountNameText;
    [SerializeField] private Button createAccountBtnTmp;

    // Geleneksel giriþ için yeni alanlar (Inspector'dan UI InputField'lere baðlayýn)
    [SerializeField] private TMP_InputField accountNameInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button signInButton; // Yeni bir Giriþ butonu ekleyin

    private void OnValidate()
    {
        // Kod çalýþmadan önce, Inspector'da UI bileþenlerini kontrol etmenizi saðlar.
        if (createAccountBtnTmp == null) Debug.LogError("createAccountBtnTmp is not assigned.");
        if (accountNameInput == null) Debug.LogError("usernameInput is not assigned.");
        if (passwordInput == null) Debug.LogError("passwordInput is not assigned.");
        if (signInButton == null) Debug.LogError("signInButton is not assigned. Consider adding a separate SignIn button.");
    }

    private async void Awake()
    {
        await UnityServices.InitializeAsync();

        // Bu noktada anonim giriþ yapmayacaðýz, oyuncunun tercihini bekleyeceðiz.
        // Hata ayýklama amaçlý, servislerin baþlatýldýðýný onaylayalým.
        Debug.Log("UGS Services Initialized.");
    }

    private void Start()
    {
        // Butonlarý ilgili metotlara baðla
        // createAccountBtnTmp: KULLANICI OLUÞTURMA (Sign Up) olarak kullanýlsýn
        createAccountBtnTmp.onClick.AddListener(SignUp);

        // signInButton: GÝRÝÞ YAPMA (Sign In) olarak kullanýlsýn
        signInButton.onClick.AddListener(SignIn);
    }

    //-------------------------------------------------------------
    // 1. KULLANICI OLUÞTURMA (Sign Up)
    //-------------------------------------------------------------
    public async void SignUp()
    {
        string username = accountNameInput.text;
        string password = passwordInput.text;

        try
        {
            // Yeni bir kullanýcý hesabý oluþtur
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            // Baþarýlý olursa, zaten giriþ yapmýþ (Signed In) demektir
            Debug.Log($"Kayýt Baþarýlý! Player ID: {AuthenticationService.Instance.PlayerId}");

            // Kullanýcý adý ve ID'sini ekrana yaz
            AccountNameText.text = $"Giriþ: {username}\nID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";

            // Ýlk kayýttan sonra veri kaydetme iþlemini baþlat (isteðe baðlý)
            SaveData();
        }
        catch (AuthenticationException ex)
        {
            // Örneðin: "Kullanýcý adý zaten kullanýmda" veya "Þifre/Kullanýcý adý çok kýsa" hatalarýný yakala
            Debug.LogError($"Kayýt Hatasý: {ex.Message}");
            AccountNameText.text = "Kayýt Hatasý: " + ex.Message.Split('\n')[0];
        }
    }

    //-------------------------------------------------------------
    // 2. GÝRÝÞ YAPMA (Sign In)
    //-------------------------------------------------------------
    public async void SignIn()
    {
        string username = accountNameInput.text;
        string password = passwordInput.text;

        try
        {
            // Mevcut bir kullanýcý hesabý ile giriþ yap
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            Debug.Log($"Giriþ Baþarýlý! Player ID: {AuthenticationService.Instance.PlayerId}");

            // Giriþ yaptýktan sonra mutlaka verileri yüklemeyi deneyin
            LoadData();

            AccountNameText.text = $"Giriþ: {username}\nID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}...";
        }
        catch (AuthenticationException ex)
        {
            // Örneðin: "Yanlýþ kullanýcý adý veya þifre"
            Debug.LogError($"Giriþ Hatasý: {ex.Message}");
            AccountNameText.text = "Giriþ Hatasý: " + ex.Message.Split('\n')[0];
        }
    }

    //-------------------------------------------------------------
    // 3. CLOUD SAVE ÝÞLEMLERÝ (Giriþ yaptýktan sonra kullanýlýr)
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
            Debug.Log("Veri baþarýyla kaydedildi!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Kaydetme hatasý: {ex.Message}");
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
            Debug.LogError($"Yükleme hatasý: {ex.Message}");
        }
    }
}
