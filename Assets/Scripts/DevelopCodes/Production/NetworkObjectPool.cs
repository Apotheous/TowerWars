using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



public class NetworkObjectPool : NetworkBehaviour
{

    [Header("Pool Ayarlarý")]
    public List<GameObject> prefabs = new List<GameObject>(); // Editörden ekleyeceðin prefabs
    public int initialAmountPerPrefab = 5; // Her prefab için baþlangýç miktarý 

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Start()
    {

        InitializePool();
    }

    private void InitializePool()
    {
        foreach (var prefab in prefabs)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < initialAmountPerPrefab; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.transform.SetParent(gameObject.transform); // Pool container altýna koy
                obj.gameObject.tag = gameObject.tag; // Tag ekle
                obj.SetActive(false);

                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(prefab, objectPool);
        }
    }

    public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            Debug.LogWarning("Prefab poolda yok: " + prefab.name);
            return null;
        }

        GameObject obj;
        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }

        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);

        if (poolDictionary.ContainsKey(obj))
        {
            poolDictionary[obj].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}
