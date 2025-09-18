using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayersPlacement : NetworkBehaviour
{

    [SerializeField] private List<Transform> spawnPoints;
    void Start()
    {
        StartCoroutine(PlacePlayersDelayed());
    }

    IEnumerator PlacePlayersDelayed()
    {
        yield return new WaitForSeconds(1f);
        PlaceLocalPlayer();
    }

    public void PlaceLocalPlayer()
    {
        if (NetworkManager.Singleton!=null)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            int index = (int)NetworkManager.Singleton.LocalClientId % spawnPoints.Count;

            localPlayer.transform.SetPositionAndRotation(spawnPoints[index].position, spawnPoints[index].rotation);
        }
       
    }

}



