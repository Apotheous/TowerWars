using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DevSingletonTransform : MonoBehaviour
{
    public static DevSingletonTransform instance;
    public Transform player1Transform, player2Transform,publicTransform;

    //[SerializeField] private List<Transform> spawnPoints;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            
        }
        else
        {
            Destroy(gameObject); // Çift oluþumu engelle
        }
    }
  

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
        if (NetworkManager.Singleton != null)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            //int index = (int)NetworkManager.Singleton.LocalClientId % spawnPoints.Count;

            //localPlayer.transform.SetPositionAndRotation(spawnPoints[index].position, spawnPoints[index].rotation);
            //localPlayer.gameObject.name = "Player_" + NetworkManager.Singleton.LocalClientId;
            
            if (NetworkManager.Singleton.LocalClientId == 1)
            {
                localPlayer.transform.SetPositionAndRotation(player1Transform.position, player1Transform.rotation);
                localPlayer.gameObject.name = "Player_" + NetworkManager.Singleton.LocalClientId;
            }
            if (NetworkManager.Singleton.LocalClientId == 2)
            {
                localPlayer.transform.SetPositionAndRotation(player2Transform.position, player2Transform.rotation);
                localPlayer.gameObject.name = "Player_" + NetworkManager.Singleton.LocalClientId;
            }
        }

 
    }
}
