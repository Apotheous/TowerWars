using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Barracks : NetworkBehaviour
{
    [SerializeField] private UnitDatabase unitDatabase;

    private Queue<string> productionQueue = new Queue<string>();
    private bool isProducing = false;

    [ServerRpc(RequireOwnership = false)]
    public void QueueUnitServerRpc(string unitId)
    {
        productionQueue.Enqueue(unitId);

        if (!isProducing)

            StartCoroutine(ProduceNextUnit());


        //// Oyuncunun kaynaðý yetiyor mu?
        //if (playerResources.Gold >= data.cost)
        //{
        //    playerResources.Gold -= data.cost;
        //    productionQueue.Enqueue(unitId);

        //    if (!isProducing)
        //        StartCoroutine(ProduceNextUnit());
        //}
        //else
        //{
        //    // oyuncuya "yetersiz kaynak" mesajý gönder
        //}
    }

    private IEnumerator ProduceNextUnit()
    {
        isProducing = true;
        while (productionQueue.Count > 0)
        {
            string unitId = productionQueue.Dequeue();
            UnitData data = unitDatabase.GetById(unitId);

            yield return new WaitForSeconds(data.trainingTime);

            GameObject obj = Instantiate(
                data.prefab,
                transform.position + Vector3.forward * 2,
                Quaternion.identity
            );
            obj.GetComponent<NetworkObject>().Spawn(true);
        }
        isProducing = false;
    }
}
