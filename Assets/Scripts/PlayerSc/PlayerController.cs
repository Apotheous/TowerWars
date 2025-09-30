using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // STAT�K REFERANS S�L�ND�.
    // Art�k her PlayerController'�n kendi �retim y�neticisi referans� var.
    private PlayerProductionManagement myProductionManager;

    private void Awake()
    {
        // Kendi objemin alt�ndaki/�zerindeki �retim y�neticisini bul.
        // Senin yap�nda GetComponentInChildren daha do�ru olacakt�r.
        myProductionManager = GetComponentInChildren<PlayerProductionManagement>();
        if (myProductionManager == null)
        {
            Debug.LogError("Bu oyuncunun PlayerProductionManagement script'i bulunamad�!", gameObject);
        }
    }

    // --- ASKER �RET�M� ---
    public void RequestUnitProduction(string unitId)
    {
        ProduceUnitServerRpc(unitId);
    }

    [ServerRpc]
    private void ProduceUnitServerRpc(string unitId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        // Statik referans yerine kendi referans�n� kullan.
        if (myProductionManager != null)
        {
            myProductionManager.StartProducingUnit(unitId, clientId);
        }
    }

    // --- TARET �RET�M� ---
    public void RequestTurretProduction(string unitId, int positionIndex)
    {
        ProduceTurretServerRpc(unitId, positionIndex);
    }

    [ServerRpc]
    private void ProduceTurretServerRpc(string unitId, int positionIndex, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        // Statik referans yerine kendi referans�n� kullan.
        if (myProductionManager != null)
        {
            myProductionManager.StartProducingTurret(unitId, positionIndex, clientId);
        }
    }
}
