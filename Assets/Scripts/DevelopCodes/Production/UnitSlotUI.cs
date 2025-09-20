using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UnitSlotUI : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button selectButton;

    private string unitId;

    public void Setup(UnitData data)
    {
        unitId = data.id;

        if (unitIcon != null)
            unitIcon.sprite = data.icon;

        if (costText != null)
            costText.text = data.cost.ToString();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => OnUnitSelected());
        }
    }

    private void OnUnitSelected()
    {
        Debug.Log($"Unit selected: {unitId}");


        // Local player GameObject
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        // Player_Game_Mode_Manager component’i
        var playerBarracks = localPlayer.GetComponent<PlayerProductionManagement>();
        if (playerBarracks == null) return;

        // Server RPC ile queue ekleme
        playerBarracks.QueueUnitServerRpc(unitId);
    }
}
