using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayersPlacement : MonoBehaviour
{
    [SerializeField] private Transform Player1Transform, Player2Transform;
    void Start()
    {
        StartCoroutine(PlacePlayersDelayed());
    }

    IEnumerator PlacePlayersDelayed()
    {
        yield return new WaitForSeconds(2f);
        PlaceLocalPlayer();
    }

    public void PlaceLocalPlayer()
    {
     

        var player1 = NetworkManager.Singleton.ConnectedClients[0].PlayerObject;
        if (player1 != null)
        {
            player1.transform.SetPositionAndRotation(Player1Transform.position, Player1Transform.rotation);
            Debug.Log("Player1 yerleþtirildi.");
        }

        var player2 = NetworkManager.Singleton.ConnectedClients[1].PlayerObject;
        if (player2 != null)
        {
            player2.transform.SetPositionAndRotation(Player2Transform.position, Player2Transform.rotation);
            Debug.Log("Player2 yerleþtirildi.");
        }
        
    }


}
