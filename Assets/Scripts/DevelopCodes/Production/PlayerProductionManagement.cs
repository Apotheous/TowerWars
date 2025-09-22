using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerProductionManagement : NetworkBehaviour
{
    [SerializeField] private UnitDatabase unitDatabase;
    [SerializeField] private TurretDatabase turretDatabase;
    [SerializeField] private PlayerSC playerSC;

    private Queue<string> productionQueue = new Queue<string>(); // Soldier queue
    private Queue<string> productionTurretQueue = new Queue<string>(); // Turret IDs
    private Queue<Transform> productionTurretQueueTransform = new Queue<Transform>(); // Turret spawn points

    private bool isProducingUnit = false;
    private bool isProducingTurret = false;

    [SerializeField] private Transform mySpawnPoint;

    [SerializeField] private Transform turretPos1;
    [SerializeField] private Transform turretPos2;
    [SerializeField] private Transform turretPos3;

    #region Soldier Production
    [ServerRpc(RequireOwnership = false)]
    public void QueueUnitServerRpc(string unitId)
    {
        UnitData data = unitDatabase.GetById(unitId);

        if (data == null)
        {
            Debug.LogWarning($"[Unit] ID {unitId} bulunamadý!");
            return;
        }

        // Kaynak kontrolü
        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            playerSC.myCurrentScrap.Value -= data.cost;
            productionQueue.Enqueue(unitId);

            if (!isProducingUnit)
                StartCoroutine(ProduceNextUnit());
        }
        else
        {
            Debug.Log("[Unit] Yetersiz kaynak!");
        }
    }

    private IEnumerator ProduceNextUnit()
    {
        isProducingUnit = true;

        while (productionQueue.Count > 0)
        {
            string unitId = productionQueue.Dequeue();
            UnitData data = unitDatabase.GetById(unitId);

            if (data == null)
            {
                Debug.LogWarning($"[Unit] ID {unitId} bulunamadý!");
                continue;
            }

            yield return new WaitForSeconds(data.trainingTime);

            GameObject obj = Instantiate(data.prefab, mySpawnPoint.position, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn(true);


        }

        isProducingUnit = false;
    }
    #endregion

    #region Turret Production
    [ServerRpc(RequireOwnership = false)]
    public void QueueTurretServerRpc(string unitId, int spawnIndex)
    {
        TurretData data = turretDatabase.GetById(unitId);

        if (data == null)
        {
            Debug.LogWarning($"[Turret] ID {unitId} bulunamadý!");
            return;
        }

        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            playerSC.myCurrentScrap.Value -= data.cost;
            productionTurretQueue.Enqueue(unitId);

            // SpawnPoint seç
            Transform spawnPoint = GetTurretSpawn(spawnIndex);
            if (spawnPoint != null)
                productionTurretQueueTransform.Enqueue(spawnPoint);

            if (!isProducingTurret)
                StartCoroutine(ProduceNextTurret());
        }
        else
        {
            Debug.Log("[Turret] Yetersiz kaynak!");
        }
    }

    private IEnumerator ProduceNextTurret()
    {
        isProducingTurret = true;

        while (productionTurretQueue.Count > 0 && productionTurretQueueTransform.Count > 0)
        {
            string unitId = productionTurretQueue.Dequeue();
            Transform spawnPoint = productionTurretQueueTransform.Dequeue();

            TurretData data = turretDatabase.GetById(unitId);

            if (data == null)
            {
                Debug.LogWarning($"[Turret] ID {unitId} bulunamadý!");
                continue;
            }

            yield return new WaitForSeconds(data.trainingTime);

            GameObject obj = Instantiate(data.prefab, spawnPoint.position, spawnPoint.rotation);
            obj.GetComponent<NetworkObject>().Spawn(true);

          
        }

        isProducingTurret = false;
    }

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
    #endregion

}
