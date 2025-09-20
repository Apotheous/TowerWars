using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerProductionManagement : NetworkBehaviour
{
    [SerializeField] private UnitDatabase unitDatabase;
    [SerializeField] private TurretDatabase turretDatabase;
    [SerializeField] private PlayerSC playerSC;


    private Queue<string> productionQueue = new Queue<string>();
   
    private bool isProducing = false;

    [SerializeField] Transform mySpawnPoint;

    

    [SerializeField] Transform turretPos1;
    [SerializeField] Transform turretPos2;
    [SerializeField] Transform turretPos3;


    [ServerRpc(RequireOwnership = false)]
    public void QueueUnitServerRpc(string unitId)
    {
        UnitData data = unitDatabase.GetById(unitId); // ID ile unit verisini al

        if (data == null)
        {
            Debug.LogWarning($"Unit with ID {unitId} not found in UnitDatabase!");
            return;
        }

        // Oyuncunun kaynaðý yeterli mi?
        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            playerSC.myCurrentScrap.Value -= data.cost;
            productionQueue.Enqueue(unitId);

            if (!isProducing)
                StartCoroutine(ProduceNextUnit());
        }
        else
        {
            Debug.Log("Yetersiz kaynak! Birim üretilemiyor.");
        }
    }

    private IEnumerator ProduceNextUnit()
    {
        isProducing = true;

        while (productionQueue.Count > 0)
        {
            string unitId = productionQueue.Dequeue();
            UnitData data = unitDatabase.GetById(unitId);

            if (data == null)
            {
                Debug.LogWarning($"Unit with ID {unitId} not found in UnitDatabase!");
                continue;
            }

            // Eðitim süresini bekle
            yield return new WaitForSeconds(data.trainingTime);

            // Birimi instantiate et ve networkte spawn et
            GameObject obj = Instantiate(data.prefab, mySpawnPoint.position, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn(true);
        }

        isProducing = false;
    }
}
