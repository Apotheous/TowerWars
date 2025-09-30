using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Bu script, eklendiði NetworkObject'in sahibi olan client için
// belirtilen GameObject'leri ve Component'leri aktif eder,
// sahibi olmayanlar için deaktif eder.

public class EnableObjectsForOwner : NetworkBehaviour
{
    [Header("Sadece Sahibi Ýçin Aktif Edilecekler")]
    [Tooltip("Sadece bu objenin sahibi olan client'ta aktif olacak GameObject'ler.")]
    [SerializeField]
    private List<GameObject> objectsToEnable = new List<GameObject>();

    [Tooltip("Sadece bu objenin sahibi olan client'ta aktif olacak Component'ler (script'ler, audio listener vb.).")]
    [SerializeField]
    private List<MonoBehaviour> componentsToEnable = new List<MonoBehaviour>();

    public override void OnNetworkSpawn()
    {
        // IsOwner, bu NetworkObject'in yerel oyuncuya ait olup olmadýðýný kontrol eder.
        bool amIOwner = IsOwner;

        foreach (var obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(amIOwner);
            }
        }

        foreach (var component in componentsToEnable)
        {
            if (component != null)
            {
                component.enabled = amIOwner;
            }
        }
    }
}
