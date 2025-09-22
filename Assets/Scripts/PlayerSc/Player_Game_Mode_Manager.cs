using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_Game_Mode_Manager : NetworkBehaviour
{
    [SerializeField] private PlayerComponentController playerComponentController;
    [SerializeField] private GameObject myCam;
    public enum PlayerMode
    {
        MainMenu,
        OneVsOne
    }

    [SerializeField] private GameObject oneVSOneMode;
    public enum PlayerAge
    {
        IceAge,
        MediavalAge,
        ModernAge
    }
    [SerializeField] public PlayerAge age;
    // Senkronize değişken
    private NetworkVariable<PlayerMode> _currentMode = new NetworkVariable<PlayerMode>(PlayerMode.MainMenu);

    public event Action<PlayerMode> OnModeChanged;

    public PlayerMode CurrentMode
    {
        get => _currentMode.Value;
        private set
        {
            if (_currentMode.Value != value)
            {
                _currentMode.Value = value;
                OnModeChanged?.Invoke(value);
                HandleModeChange(value);
            }
        }
    }


    private void Start()
    {
        CurrentMode.Equals(PlayerMode.MainMenu);
        _currentMode.OnValueChanged += (oldValue, newValue) =>
        {
            OnModeChanged?.Invoke(newValue);
            HandleModeChange(newValue);
        };

        HandleModeChange(_currentMode.Value);
    }

    private void HandleModeChange(PlayerMode newMode)
    {
        switch (newMode)
        {
            case PlayerMode.MainMenu:
                if (oneVSOneMode) oneVSOneMode.SetActive(false);
                playerComponentController.SetComponentsActive(false);
                break;
            case PlayerMode.OneVsOne:
                if (oneVSOneMode) oneVSOneMode.SetActive(true);
                playerComponentController.SetComponentsActive(true);
                if (IsOwner)
                {
                    myCam.SetActive(true);
                }else
                {
                    myCam.SetActive(false);
                }

                break;
        }
    }

    // ✅ Client sadece "başlamak istiyorum" diye talepte bulunur
    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Player {senderId} requested to start game.");

        // Burada sunucu kontrol eder → örn: herkes hazır mı?
        if (AllPlayersReady())
        {
            Debug.Log("All players ready, starting game!");
            CurrentMode = PlayerMode.OneVsOne;
        }
        else
        {
            Debug.Log("Start request denied: not all players are ready.");
        }
    }

    // ✅ Dummy kontrol, sonra kendi mantığını koyabilirsin
    private bool AllPlayersReady()
    {
        // Şimdilik sadece "en az 2 oyuncu var mı" diye bakalım
        return NetworkManager.Singleton.ConnectedClients.Count >= 2;
    }

}
