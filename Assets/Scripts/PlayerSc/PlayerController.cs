using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // STAT�K REFERANS S�L�ND�.
    // Art�k her PlayerController'�n kendi �retim y�neticisi referans� var.
    private PlayerProductionManagement myProductionManager;
    private PlayerSC myPlayerSc;

    private void Awake()
    {
        // Kendi objemin alt�ndaki/�zerindeki �retim y�neticisini bul.
        // Senin yap�nda GetComponentInChildren daha do�ru olacakt�r.
        myProductionManager = GetComponentInChildren<PlayerProductionManagement>();
        Debug.Log("PlayerController Awake: �retim y�neticisi referans� atand�.adi =="+ myProductionManager.name);
        if (myProductionManager == null)
        {
            Debug.LogError("Bu oyuncunun PlayerProductionManagement script'i bulunamad�!", gameObject);
        }
        myPlayerSc = GetComponent<PlayerSC>();
        Debug.Log("PlayerController Awake: PlayerSC referans� atand�.adi ==" + myPlayerSc.name);
        if (myPlayerSc == null)
        {
            Debug.LogError("Bu oyuncunun PlayerSC script'i bulunamad�!", gameObject);
        }
    }

    // --- Level Up ---
    public void RequestPlayerLevelUp( )
    {
        PlayerLevelUpServerRpc();
    }
    [ServerRpc]
    private void PlayerLevelUpServerRpc( ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        // Statik referans yerine kendi referans�n� kullan.
        if (myProductionManager != null)
        {
            //myProductionManager.StartProducingUnit( clientId);
            //myPlayerSc.HandleTechPointChanged();
            if(IsServer)
            {
                Debug.Log("PlayerLevelUpServerRpc �a�r�ld�.Server " +  ", clientId: " + clientId);
            }

            Debug.Log("PlayerLevelUpServerRpc �a�r�ld�. Client" + ", clientId: " + clientId);
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
