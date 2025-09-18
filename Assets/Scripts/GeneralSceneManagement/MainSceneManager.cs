using System;
using Unity.Netcode;
using UnityEngine;

public class MainSceneManager : NetworkBehaviour
{
    public static MainSceneManager Instance; 
    // Scene açıldığında diğer scriptlerin abone olabileceği event
    public static event Action OnMainSceneStarted;
    public static event Action OnMainSceneClosed;

    private void Awake()
    {
        // Singleton kurulumu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;


        Debug.Log("MainSceneManager Awake: Scene initial setup");
        // Buraya sahne açılır açılmaz yapılacak ilk ayarlar gelebilir
        
    }
    private void Start()
    {
        MainSceneWhenOpened();
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        
        if (!IsClient || !IsOwner) return; // sadece kendi için

        Debug.Log($"[MainSceneManager] Ben bağlandım → ClientId: {clientId}");

        var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObject != null)
        {
            var cam = playerObject.GetComponent<RTSCam>();
            if (cam != null)
                cam.gameObject.SetActive(false);
        }
    }
    public void MainSceneWhenOpened()
    {
        Debug.Log("MainSceneManager Start: Main scene initialized");

        // Event tetikle → başka sınıflar dinleyip kendi işlerini yapabilir
        OnMainSceneStarted?.Invoke();

    }

    public void MainSceneWhenClosed()
    {
        Debug.Log("MainSceneManager Start: Main scene initialized");

        // Event tetikle → başka sınıflar dinleyip kendi işlerini yapabilir
        OnMainSceneClosed?.Invoke();

    }

    private void InitializeSystems()
    {
        Debug.Log("MainSceneManager → sistemler başlatılıyor...");

        // Örnek: Player spawn sistemi
        // PlayerSpawnManager.Instance.SpawnLocalPlayer();

        // Örnek: UI
        // UIManager.Instance.ShowMainMenu();

        // Örnek: Audio
        // AudioManager.Instance.PlayBackgroundMusic("MainTheme");
    }



}
