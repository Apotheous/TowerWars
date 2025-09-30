using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerProductionManagement : NetworkBehaviour
{
    [SerializeField] private UnitDatabase unitDatabase;
    [SerializeField] private TurretDatabase turretDatabase;
    [SerializeField] private PlayerSC playerSC;

    //private Queue<string> productionQueue = new Queue<string>(); // Soldier queue
    private Queue<string> productionTurretQueue = new Queue<string>(); // Turret IDs
    private Queue<Transform> productionTurretQueueTransform = new Queue<Transform>(); // Turret spawn points

    // YENÝ HALÝ: (string, ulong) çiftlerini tutan bir kuyruk
    private Queue<(string unitId, ulong clientId)> productionQueue = new Queue<(string, ulong)>();

    private bool isProducingUnit = false;
    private bool isProducingTurret = false;

    [SerializeField] private Transform mySpawnPoint;

    [SerializeField] private Transform turretPos1;
    [SerializeField] private Transform turretPos2;
    [SerializeField] private Transform turretPos3;
    [SerializeField] private string myTag;

    [SerializeField] NetworkObjectPool objectPool;

    private void Awake()
    {
        // PlayerController'ýn bize kolayca ulaþabilmesi için statik referansý dolduruyoruz.
        // Sahnede sadece bir tane ProductionManager olduðunu varsayýyoruz.
        
    }
    private void OnTriggerEnter(Collider c)
    {

        if (c.GetComponent<NetworkObjectPool>()!=null)
        {
            objectPool = c.GetComponent<NetworkObjectPool>();
        }
      
    }
    #region Soldier Production

    // Bu RPC'yi çaðýrma þekli ayný kalýyor. Buton kodunu deðiþtirmene gerek yok.
    [ServerRpc(RequireOwnership = false)]
    public void QueueUnitServerRpc(string unitId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        UnitData data = unitDatabase.GetById(unitId);
        if (data == null)
        {
            Debug.LogWarning($"[Unit] ID {unitId} bulunamadý!");
            return;
        }

        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            playerSC.myCurrentScrap.Value -= data.cost;
            productionQueue.Enqueue((unitId, clientId));

            if (!isProducingUnit)
                StartCoroutine(ProduceNextUnit());
        }
    }
    // ARTIK RPC DEÐÝL! PlayerController çaðýracak.
    public void StartProducingUnit(string unitId, ulong clientId)
    {
        // ... (QueueUnitServerRpc içindeki kodun aynýsý)

        // 1. DOÐRU VERÝTABANINI KULLAN
        UnitData data = unitDatabase.GetById(unitId);
        if (data == null)
        {
            Debug.LogError($"[Unit] ID '{unitId}' unitDatabase içinde bulunamadý!");
            return;
        }

        // 2. KAYNAK KONTROLÜNÜ YAP
        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            // 3. YETERLÝ KAYNAK VARSA:
            // Önce kaynaðý düþür
            playerSC.myCurrentScrap.Value -= data.cost;

            // Sonra birimi üretim sýrasýna ekle
            productionQueue.Enqueue((unitId, clientId));

            // Üretim zaten baþlamadýysa, baþlat
            if (!isProducingUnit)
            {
                StartCoroutine(ProduceNextUnit());
            }
        }
        else
        {
            // 4. YETERLÝ KAYNAK YOKSA:
            // Ýsteðe baðlý: Oyuncuya "Yetersiz Kaynak" mesajý göster.
            Debug.Log($"[Unit] Yetersiz kaynak! {data.cost} scrap gerekli ama {playerSC.myCurrentScrap.Value} scrap var.");
        }


    }

    // ARTIK RPC DEÐÝL! PlayerController çaðýracak.
    public void StartProducingTurret(string unitId, int positionIndex, ulong clientId)
    {
        TurretData data = turretDatabase.GetById(unitId);
        if (data == null)
        {
            Debug.LogError($"[Turret] ID '{unitId}' turretDatabase içinde bulunamadý!");
            return;
        }

        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            playerSC.myCurrentScrap.Value -= data.cost;
            productionTurretQueue.Enqueue(unitId);
            Transform spawnPoint = GetTurretSpawn(positionIndex);
            if (spawnPoint != null)
                productionTurretQueueTransform.Enqueue(spawnPoint);

            if (!isProducingTurret)
                StartCoroutine(ProduceNextTurret());
        }
        else
        {
            Debug.Log($"[Turret] Yetersiz kaynak! {data.cost} scrap gerekli ama {playerSC.myCurrentScrap.Value} scrap var.");
        }
    }
    private IEnumerator ProduceNextUnit()
    {
        isProducingUnit = true;

        while (productionQueue.Count > 0)
        {
            var productionOrder = productionQueue.Dequeue();
            string unitId = productionOrder.unitId;
            ulong ownerId = productionOrder.clientId;

            UnitData data = unitDatabase.GetById(unitId);
            if (data == null)
            {
                Debug.LogWarning($"[Unit] ID {unitId} bulunamadý!");
                continue;
            }

            yield return new WaitForSeconds(data.trainingTime);

            // --- DEÐÝÞÝKLÝK BURADA BAÞLIYOR ---

            // ADIM 1: Havuzdan almak yerine doðrudan Instantiate ediyoruz.
            GameObject obj = Instantiate(data.prefab, mySpawnPoint.position, Quaternion.identity);

            if (obj != null)
            {
                var unitIdentity = obj.GetComponent<UnitIdentity>();
                if (unitIdentity != null)
                {
                    // ADIM 2: Takým kimliðini (TeamId) atýyoruz.
                    unitIdentity.TeamId.Value = (int)ownerId;
                    Debug.Log($"Birim Instantiate edildi. Sahibi: Client {ownerId}, TeamId atandý: {unitIdentity.TeamId.Value}");
                }
                else
                {
                    Debug.LogError($"'{data.prefab.name}' prefab'ýnda UnitIdentity script'i bulunmuyor!");
                }

                // ADIM 3: Objeyi network'e spawn ediyoruz.
                // Bu sayede client'lar objeyi, TeamId'si atanmýþ güncel haliyle alýrlar.
                obj.GetComponent<NetworkObject>().Spawn(true);
            }
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

            GameObject obj = objectPool.GetFromPool(data.prefab, spawnPoint.position, spawnPoint.rotation);
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
