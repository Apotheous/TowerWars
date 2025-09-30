using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // STATÝK REFERANS SÝLÝNDÝ.
    // Artýk her PlayerController'ýn kendi üretim yöneticisi referansý var.
    private PlayerProductionManagement myProductionManager;

    private void Awake()
    {
        // Kendi objemin altýndaki/üzerindeki üretim yöneticisini bul.
        // Senin yapýnda GetComponentInChildren daha doðru olacaktýr.
        myProductionManager = GetComponentInChildren<PlayerProductionManagement>();
        if (myProductionManager == null)
        {
            Debug.LogError("Bu oyuncunun PlayerProductionManagement script'i bulunamadý!", gameObject);
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
