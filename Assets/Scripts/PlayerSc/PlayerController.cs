using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // STATÝK REFERANS SÝLÝNDÝ.
    // Artýk her PlayerController'ýn kendi üretim yöneticisi referansý var.
    private PlayerProductionManagement myProductionManager;
    private PlayerSC myPlayerSc;

    private void Awake()
    {
        // Kendi objemin altýndaki/üzerindeki üretim yöneticisini bul.
        // Senin yapýnda GetComponentInChildren daha doðru olacaktýr.
        myProductionManager = GetComponentInChildren<PlayerProductionManagement>();
        Debug.Log("PlayerController Awake: Üretim yöneticisi referansý atandý.adi =="+ myProductionManager.name);
        if (myProductionManager == null)
        {
            Debug.LogError("Bu oyuncunun PlayerProductionManagement script'i bulunamadý!", gameObject);
        }
        myPlayerSc = GetComponent<PlayerSC>();
        Debug.Log("PlayerController Awake: PlayerSC referansý atandý.adi ==" + myPlayerSc.name);
        if (myPlayerSc == null)
        {
            Debug.LogError("Bu oyuncunun PlayerSC script'i bulunamadý!", gameObject);
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

        // Statik referans yerine kendi referansýný kullan.
        if (myProductionManager != null)
        {
            //myProductionManager.StartProducingUnit( clientId);
            //myPlayerSc.HandleTechPointChanged();
            if(IsServer)
            {
                Debug.Log("PlayerLevelUpServerRpc çaðrýldý.Server " +  ", clientId: " + clientId);
            }

            Debug.Log("PlayerLevelUpServerRpc çaðrýldý. Client" + ", clientId: " + clientId);
        }
    }

    // --- ASKER ÜRETÝMÝ ---
    public void RequestUnitProduction(string unitId)
    {
        ProduceUnitServerRpc(unitId);
    }

    [ServerRpc]
    private void ProduceUnitServerRpc(string unitId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        // Statik referans yerine kendi referansýný kullan.
        if (myProductionManager != null)
        {
            myProductionManager.StartProducingUnit(unitId, clientId);
        }
    }

    // --- TARET ÜRETÝMÝ ---
    public void RequestTurretProduction(string unitId, int positionIndex)
    {
        ProduceTurretServerRpc(unitId, positionIndex);
    }

    [ServerRpc]
    private void ProduceTurretServerRpc(string unitId, int positionIndex, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        // Statik referans yerine kendi referansýný kullan.
        if (myProductionManager != null)
        {
            myProductionManager.StartProducingTurret(unitId, positionIndex, clientId);
        }
    }
}
