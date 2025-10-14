using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerProductionManagement : NetworkBehaviour
{
    [SerializeField] private UnitDatabase unitDatabase;
    [SerializeField] private TurretDatabase turretDatabase;
    [SerializeField] private PlayerSC playerSC;
    [SerializeField] private Transform mySpawnPoint;
    [SerializeField] private Transform turretPos1, turretPos2, turretPos3;

    // Asker ve Taret için üretim sýralarý
    private Queue<(string unitId, ulong clientId)> productionUnitQueue = new Queue<(string, ulong)>();
    private Queue<(string unitId, int positionIndex, ulong clientId)> productionTurretQueue = new Queue<(string, int, ulong)>();

    private bool isProducingUnit = false;
    private bool isProducingTurret = false;

    

    #region Üretim Baþlatma Metodlarý (PlayerController'dan çaðrýlýr)

    public void StartProducingUnit(string unitId, ulong clientId)
    {
        UnitData data = unitDatabase.GetById(unitId);
        if (data == null) { Debug.LogError($"[Unit] ID '{unitId}' bulunamadý!"); return; }

        if (playerSC.GetMyCurrentScrap() >= data.cost)
        {
            playerSC.UpdateMyScrap(data.cost);
            productionUnitQueue.Enqueue((unitId, clientId));
            if (!isProducingUnit) StartCoroutine(ProduceNextUnit());
        }
        else
        {
            Debug.Log($"[Unit] Yetersiz kaynak!");
        }
    }

    public void StartProducingTurret(string unitId, int positionIndex, ulong clientId)
    {
        TurretData data = turretDatabase.GetById(unitId);
        if (data == null) { Debug.LogError($"[Turret] ID '{unitId}' bulunamadý!"); return; }

        if (playerSC.GetMyCurrentScrap() >= data.cost)
        {
            playerSC.UpdateMyScrap(data.cost);
            // Sýraya artýk pozisyon ve sahip bilgisini de ekliyoruz
            productionTurretQueue.Enqueue((unitId, positionIndex, clientId));
            if (!isProducingTurret) StartCoroutine(ProduceNextTurret());
        }
        else
        {
            Debug.Log($"[Turret] Yetersiz kaynak!");
        }
    }

    #endregion

    #region Üretim Coroutine'leri (Sadece Server'da çalýþýr)

    private IEnumerator ProduceNextUnit()
    {
        isProducingUnit = true;
        while (productionUnitQueue.Count > 0)
        {
            var order = productionUnitQueue.Dequeue();
            UnitData data = unitDatabase.GetById(order.unitId);
            if (data == null) continue;

            yield return new WaitForSeconds(data.trainingTime);

            GameObject obj = Instantiate(data.prefab, mySpawnPoint.position, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn(true);
            
            var unitIdentity = obj.GetComponent<Soldier>();
            if (unitIdentity != null)
            {
                unitIdentity.TeamId.Value = (int)order.clientId;
                obj.name = $"Soldier_Team_{order.clientId}";
            }
            
        }
        isProducingUnit = false;
    }

    private IEnumerator ProduceNextTurret()
    {
        isProducingTurret = true;
        while (productionTurretQueue.Count > 0)
        {
            var order = productionTurretQueue.Dequeue();
            TurretData data = turretDatabase.GetById(order.unitId);
            if (data == null) continue;

            Transform spawnPoint = GetTurretSpawn(order.positionIndex);
            if (spawnPoint == null) continue;

            yield return new WaitForSeconds(data.trainingTime);

            // Taretler için de artýk doðrudan Instantiate kullanýyoruz
            GameObject obj = Instantiate(data.prefab, spawnPoint.position, spawnPoint.rotation);

            // Taretlerin de kime ait olduðunu belirtiyoruz

            obj.GetComponent<NetworkObject>().Spawn(true);
            var unitIdentity = obj.GetComponent<Turret>();
            if (unitIdentity != null)
            {
                unitIdentity.TeamId.Value = (int)order.clientId;
            }
        }
        isProducingTurret = false;
    }

    #endregion

    private Transform GetTurretSpawn(int index)
    {
        switch (index)
        {
            case 1: return turretPos1;
            case 2: return turretPos2;
            case 3: return turretPos3;
            default: return mySpawnPoint;
        }
    }

}
