using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_Game_Mode_Manager : NetworkBehaviour
{
    [SerializeField] private PlayerComponentController playerComponentController;
    public enum PlayerMode
    {
        MainMenu,
        OneVsOne
    }

    [SerializeField] private GameObject oneVSOneMode;

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



    //private void Start()
    //{
    //    // NetworkVariable değişikliklerini dinle
    //    _currentMode.OnValueChanged += (oldValue, newValue) =>
    //    {
    //        OnModeChanged?.Invoke(newValue);
    //        HandleModeChange(newValue);
    //    };
    //    //HandleModeChange(PlayerMode.MainMenu);
    //    playerComponentController.SetComponentsActive(false);
    //}

    //private void HandleModeChange(PlayerMode newMode)
    //{
    //    switch (newMode)
    //    {
    //        case PlayerMode.MainMenu:
    //            Debug.Log("Mode changed to MainMenu");
    //            if (oneVSOneMode) oneVSOneMode.SetActive(false);
    //            playerComponentController.SetComponentsActive(false);
    //            break;

    //        case PlayerMode.OneVsOne:
    //            Debug.Log("Mode changed to OneVsOne");
    //            if (oneVSOneMode) oneVSOneMode.SetActive(true);
    //            playerComponentController.SetComponentsActive(true);
    //            break;
    //    }
    //}

    //// Client bir değişiklik yapmak istediğinde server üzerinden değiştir
    //[ServerRpc]
    //public void SetModeServerRpc(PlayerMode newMode)
    //{
    //    currentMode = newMode;
    //}






}
