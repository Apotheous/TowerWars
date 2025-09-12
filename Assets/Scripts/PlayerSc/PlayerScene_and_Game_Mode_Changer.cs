using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerScene_and_Game_Mode_Changer : NetworkBehaviour
{
    public enum PlayerMode
    {
        MainMenu,
        OneVsOne
    }

    [SerializeField] private GameObject oneVSOneMode;

    // NetworkVariable ile senkronizasyon
    public NetworkVariable<PlayerMode> _currentMode = new NetworkVariable<PlayerMode>(PlayerMode.MainMenu);

    // Event: mode deðiþtiðinde diðer scriptler dinleyebilir
    public event Action<PlayerMode> OnModeChanged;

    public PlayerMode currentMode
    {
        get => _currentMode.Value;
        private set
        {
            if (_currentMode.Value != value)
            {
                _currentMode.Value = value;
                OnModeChanged?.Invoke(_currentMode.Value);
                HandleModeChange(_currentMode.Value);
            }
        }
    }

    private void Start()
    {
        // NetworkVariable deðiþikliklerini dinle
        _currentMode.OnValueChanged += (oldValue, newValue) =>
        {
            OnModeChanged?.Invoke(newValue);
            HandleModeChange(newValue);
        };
    }

    private void HandleModeChange(PlayerMode newMode)
    {
        switch (newMode)
        {
            case PlayerMode.MainMenu:
                Debug.Log("Mode changed to MainMenu");
                if (oneVSOneMode) oneVSOneMode.SetActive(false);
                break;

            case PlayerMode.OneVsOne:
                Debug.Log("Mode changed to OneVsOne");
                if (oneVSOneMode) oneVSOneMode.SetActive(true);
                break;
        }
    }

    // Client bir deðiþiklik yapmak istediðinde server üzerinden deðiþtir
    [ServerRpc]
    public void SetModeServerRpc(PlayerMode newMode)
    {
        currentMode = newMode;
    }






}
