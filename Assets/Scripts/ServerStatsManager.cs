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

    // Oyuncu say�s� ve durum bilgileri
    private int currentPlayerCount = 0;
    private bool isConnected = false;
    private List<string> connectedPlayerIds = new List<string>();

    void Start()
    {
        // NetworkManager event'lerini dinlemeye ba�la
        SubscribeToNetworkEvents();

        // �lk durumu g�ncelle
        UpdateUI();
    }

    /// <summary>
    /// NetworkManager event'lerini dinlemeye ba�la
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            // Oyuncu ba�land���nda
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;

            // Oyuncu ayr�ld���nda  
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;

            // Sunucu ba�lad���nda
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            // Client olarak ba�land���nda
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }
    }

    /// <summary>
    /// Oyuncu sunucuya ba�land���nda �a�r�l�r
    /// </summary>
    private void OnPlayerConnected(ulong clientId)
    {
        Debug.Log($"Oyuncu ba�land�: {clientId}");

        // Oyuncu listesine ekle
        string playerId = clientId.ToString();
        if (!connectedPlayerIds.Contains(playerId))
        {
            connectedPlayerIds.Add(playerId);
        }

        // Sayac� g�ncelle
        UpdatePlayerCount();
    }

    /// <summary>
    /// Oyuncu sunucudan ayr�ld���nda �a�r�l�r
    /// </summary>
    private void OnPlayerDisconnected(ulong clientId)
    {
        Debug.Log($"Oyuncu ayr�ld�: {clientId}");

        // Oyuncu listesinden ��kar
        string playerId = clientId.ToString();
        connectedPlayerIds.Remove(playerId);

        // Sayac� g�ncelle
        UpdatePlayerCount();
    }

    /// <summary>
    /// Sunucu ba�lad���nda �a�r�l�r
    /// </summary>
    private void OnServerStarted()
    {
        Debug.Log("Sunucu ba�lat�ld�");
        isConnected = true;
        UpdateUI();
    }

    /// <summary>
    /// Client olarak ba�land���nda �a�r�l�r
    /// </summary>
    private void OnClientStarted()
    {
        Debug.Log("Client olarak ba�lan�ld�");
        isConnected = true;
        UpdateUI();
    }

    /// <summary>
    /// Oyuncu say�s�n� g�nceller
    /// </summary>
    private void UpdatePlayerCount()
    {
        if (NetworkManager.Singleton != null)
        {
            // Ba�l� oyuncu say�s�n� al
            currentPlayerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            Debug.Log($"G�ncel oyuncu say�s�: {currentPlayerCount}");
        }

        // UI'yi g�ncelle
        UpdateUI();
    }

    /// <summary>
    /// UI elementlerini g�nceller
    /// </summary>
    private void UpdateUI()
    {
        // Oyuncu say�s�n� g�ster
        if (playerCountText != null)
        {
            playerCountText.text = $"Online Oyuncular: {currentPlayerCount}";
        }

        // Ba�lant� durumunu g�ster
        if (connectionStatusText != null)
        {
            string status = GetConnectionStatus();
            connectionStatusText.text = $"Durum: {status}";
        }

        // Oyuncu listesini g�ster
        if (playerListText != null)
        {
            UpdatePlayerList();
        }
    }

    /// <summary>
    /// Mevcut ba�lant� durumunu string olarak d�nd�r�r
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
            return "Ba�lant�s�z";
        }
    }

    /// <summary>
    /// Ba�l� oyuncular�n listesini g�nceller
    /// </summary>
    private void UpdatePlayerList()
    {
        if (playerListText == null) return;

        string playerList = "Ba�l� Oyuncular:\n";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                playerList += $"- Player {client.ClientId}\n";
            }
        }
        else
        {
            playerList += "Hi� oyuncu yok";
        }

        playerListText.text = playerList;
    }

    // Public metodlar - D��ar�dan kullan�labilir

    /// <summary>
    /// �u anki oyuncu say�s�n� d�nd�r�r
    /// </summary>
    public int GetPlayerCount()
    {
        return currentPlayerCount;
    }

    /// <summary>
    /// Sunucuya ba�l� olup olmad���n� d�nd�r�r
    /// </summary>
    public bool IsConnectedToServer()
    {
        return isConnected && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    }

    /// <summary>
    /// Sunucu mu oldu�unu d�nd�r�r
    /// </summary>
    public bool IsServer()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }

    /// <summary>
    /// Maksimum oyuncu say�s�n� d�nd�r�r (�rnek)
    /// </summary>
    public int GetMaxPlayerCount()
    {
        // Bu de�eri ihtiyac�na g�re ayarlayabilirsin
        return 4;
    }

    /// <summary>
    /// Sunucu dolu mu kontrol� yapar
    /// </summary>
    public bool IsServerFull()
    {
        return currentPlayerCount >= GetMaxPlayerCount();
    }

    void Update()
    {
        // Her saniye UI'yi g�ncelle (performans i�in s�n�rl�)
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
