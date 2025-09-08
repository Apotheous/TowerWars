using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private List<ulong> connectedPlayers = new List<ulong>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // sahne deðiþince kaybolmasýn
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!connectedPlayers.Contains(clientId))
        {
            connectedPlayers.Add(clientId);
            Debug.Log($"Client {clientId} connected. Total: {connectedPlayers.Count}");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (connectedPlayers.Contains(clientId))
        {
            connectedPlayers.Remove(clientId);
            Debug.Log($"Client {clientId} disconnected. Total: {connectedPlayers.Count}");
        }
    }

    public List<ulong> GetConnectedPlayers()
    {
        return connectedPlayers;
    }
}
