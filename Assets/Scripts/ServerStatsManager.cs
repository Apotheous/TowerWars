using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ServerStatsManager : MonoBehaviour
{
    [Header("UI Elementleri")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI playerListText;

    // Oyuncu sayýsý ve durum bilgileri
    private int currentPlayerCount = 0;
    private bool isConnected = false;
    private List<string> connectedPlayerIds = new List<string>();

    void Start()
    {
        // NetworkManager event'lerini dinlemeye baþla
        SubscribeToNetworkEvents();

        // Ýlk durumu güncelle
        UpdateUI();
    }

    /// <summary>
    /// NetworkManager event'lerini dinlemeye baþla
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            // Oyuncu baðlandýðýnda
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;

            // Oyuncu ayrýldýðýnda  
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;

            // Sunucu baþladýðýnda
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            // Client olarak baðlandýðýnda
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }
    }

    /// <summary>
    /// Oyuncu sunucuya baðlandýðýnda çaðrýlýr
    /// </summary>
    private void OnPlayerConnected(ulong clientId)
    {
        Debug.Log($"Oyuncu baðlandý: {clientId}");

        // Oyuncu listesine ekle
        string playerId = clientId.ToString();
        if (!connectedPlayerIds.Contains(playerId))
        {
            connectedPlayerIds.Add(playerId);
        }

        // Sayacý güncelle
        UpdatePlayerCount();
    }

    /// <summary>
    /// Oyuncu sunucudan ayrýldýðýnda çaðrýlýr
    /// </summary>
    private void OnPlayerDisconnected(ulong clientId)
    {
        Debug.Log($"Oyuncu ayrýldý: {clientId}");

        // Oyuncu listesinden çýkar
        string playerId = clientId.ToString();
        connectedPlayerIds.Remove(playerId);

        // Sayacý güncelle
        UpdatePlayerCount();
    }

    /// <summary>
    /// Sunucu baþladýðýnda çaðrýlýr
    /// </summary>
    private void OnServerStarted()
    {
        Debug.Log("Sunucu baþlatýldý");
        isConnected = true;
        UpdateUI();
    }

    /// <summary>
    /// Client olarak baðlandýðýnda çaðrýlýr
    /// </summary>
    private void OnClientStarted()
    {
        Debug.Log("Client olarak baðlanýldý");
        isConnected = true;
        UpdateUI();
    }

    /// <summary>
    /// Oyuncu sayýsýný günceller
    /// </summary>
    private void UpdatePlayerCount()
    {
        if (NetworkManager.Singleton != null)
        {
            // Baðlý oyuncu sayýsýný al
            currentPlayerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            Debug.Log($"Güncel oyuncu sayýsý: {currentPlayerCount}");
        }

        // UI'yi güncelle
        UpdateUI();
    }

    /// <summary>
    /// UI elementlerini günceller
    /// </summary>
    private void UpdateUI()
    {
        // Oyuncu sayýsýný göster
        if (playerCountText != null)
        {
            playerCountText.text = $"Online Oyuncular: {currentPlayerCount}";
        }

        // Baðlantý durumunu göster
        if (connectionStatusText != null)
        {
            string status = GetConnectionStatus();
            connectionStatusText.text = $"Durum: {status}";
        }

        // Oyuncu listesini göster
        if (playerListText != null)
        {
            UpdatePlayerList();
        }
    }

    /// <summary>
    /// Mevcut baðlantý durumunu string olarak döndürür
    /// </summary>
    private string GetConnectionStatus()
    {
        if (NetworkManager.Singleton == null)
        {
            return "NetworkManager Yok";
        }

        if (NetworkManager.Singleton.IsServer)
        {
            return "Sunucu";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            return "Client";
        }
        else
        {
            return "Baðlantýsýz";
        }
    }

    /// <summary>
    /// Baðlý oyuncularýn listesini günceller
    /// </summary>
    private void UpdatePlayerList()
    {
        if (playerListText == null) return;

        string playerList = "Baðlý Oyuncular:\n";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                playerList += $"- Player {client.ClientId}\n";
            }
        }
        else
        {
            playerList += "Hiç oyuncu yok";
        }

        playerListText.text = playerList;
    }

    // Public metodlar - Dýþarýdan kullanýlabilir

    /// <summary>
    /// Þu anki oyuncu sayýsýný döndürür
    /// </summary>
    public int GetPlayerCount()
    {
        return currentPlayerCount;
    }

    /// <summary>
    /// Sunucuya baðlý olup olmadýðýný döndürür
    /// </summary>
    public bool IsConnectedToServer()
    {
        return isConnected && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    }

    /// <summary>
    /// Sunucu mu olduðunu döndürür
    /// </summary>
    public bool IsServer()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }

    /// <summary>
    /// Maksimum oyuncu sayýsýný döndürür (örnek)
    /// </summary>
    public int GetMaxPlayerCount()
    {
        // Bu deðeri ihtiyacýna göre ayarlayabilirsin
        return 4;
    }

    /// <summary>
    /// Sunucu dolu mu kontrolü yapar
    /// </summary>
    public bool IsServerFull()
    {
        return currentPlayerCount >= GetMaxPlayerCount();
    }

    void Update()
    {
        // Her saniye UI'yi güncelle (performans için sýnýrlý)
        if (Time.frameCount % 60 == 0) // 60 FPS'te saniyede bir
        {
            UpdatePlayerCount();
        }
    }

    void OnDestroy()
    {
        // Event'leri temizle
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        }
    }
}
