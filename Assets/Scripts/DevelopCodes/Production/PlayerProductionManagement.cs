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

    // YEN� HAL�: (string, ulong) �iftlerini tutan bir kuyruk
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
        // PlayerController'�n bize kolayca ula�abilmesi i�in statik referans� dolduruyoruz.
        // Sahnede sadece bir tane ProductionManager oldu�unu varsay�yoruz.
        
    }
    private void OnTriggerEnter(Collider c)
    {

        if (c.GetComponent<NetworkObjectPool>()!=null)
        {
            objectPool = c.GetComponent<NetworkObjectPool>();
        }
      
    }
    #region Soldier Production

    // Bu RPC'yi �a��rma �ekli ayn� kal�yor. Buton kodunu de�i�tirmene gerek yok.
    [ServerRpc(RequireOwnership = false)]
    public void QueueUnitServerRpc(string unitId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        UnitData data = unitDatabase.GetById(unitId);
        if (data == null)
        {
            Debug.LogWarning($"[Unit] ID {unitId} bulunamad�!");
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
    // ARTIK RPC DE��L! PlayerController �a��racak.
    public void StartProducingUnit(string unitId, ulong clientId)
    {
        // ... (QueueUnitServerRpc i�indeki kodun ayn�s�)

        // 1. DO�RU VER�TABANINI KULLAN
        UnitData data = unitDatabase.GetById(unitId);
        if (data == null)
        {
            Debug.LogError($"[Unit] ID '{unitId}' unitDatabase i�inde bulunamad�!");
            return;
        }

        // 2. KAYNAK KONTROL�N� YAP
        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            // 3. YETERL� KAYNAK VARSA:
            // �nce kayna�� d���r
            playerSC.myCurrentScrap.Value -= data.cost;

            // Sonra birimi �retim s�ras�na ekle
            productionQueue.Enqueue((unitId, clientId));

            // �retim zaten ba�lamad�ysa, ba�lat
            if (!isProducingUnit)
            {
                StartCoroutine(ProduceNextUnit());
            }
        }
        else
        {
            // 4. YETERL� KAYNAK YOKSA:
            // �ste�e ba�l�: Oyuncuya "Yetersiz Kaynak" mesaj� g�ster.
            Debug.Log($"[Unit] Yetersiz kaynak! {data.cost} scrap gerekli ama {playerSC.myCurrentScrap.Value} scrap var.");
        }


    }

    // ARTIK RPC DE��L! PlayerController �a��racak.
    public void StartProducingTurret(string unitId, int positionIndex, ulong clientId)
    {
        TurretData data = turretDatabase.GetById(unitId);
        if (data == null)
        {
            Debug.LogError($"[Turret] ID '{unitId}' turretDatabase i�inde bulunamad�!");
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
                Debug.LogWarning($"[Unit] ID {unitId} bulunamad�!");
                continue;
            }

            yield return new WaitForSeconds(data.trainingTime);

            // --- DE����KL�K BURADA BA�LIYOR ---

            // ADIM 1: Havuzdan almak yerine do�rudan Instantiate ediyoruz.
            GameObject obj = Instantiate(data.prefab, mySpawnPoint.position, Quaternion.identity);

            if (obj != null)
            {
                var unitIdentity = obj.GetComponent<UnitIdentity>();
                if (unitIdentity != null)
                {
                    // ADIM 2: Tak�m kimli�ini (TeamId) at�yoruz.
                    unitIdentity.TeamId.Value = (int)ownerId;
                    Debug.Log($"Birim Instantiate edildi. Sahibi: Client {ownerId}, TeamId atand�: {unitIdentity.TeamId.Value}");
                }
                else
                {
                    Debug.LogError($"'{data.prefab.name}' prefab'�nda UnitIdentity script'i bulunmuyor!");
                }

                // ADIM 3: Objeyi network'e spawn ediyoruz.
                // Bu sayede client'lar objeyi, TeamId'si atanm�� g�ncel haliyle al�rlar.
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
            Debug.LogWarning($"[Turret] ID {unitId} bulunamad�!");
            return;
        }

        if (playerSC.myCurrentScrap.Value >= data.cost)
        {
            playerSC.myCurrentScrap.Value -= data.cost;
            productionTurretQueue.Enqueue(unitId);

            // SpawnPoint se�
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
                Debug.LogWarning($"[Turret] ID {unitId} bulunamad�!");
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
