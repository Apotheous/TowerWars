using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayersPlacement : NetworkBehaviour
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
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayer != null)
        {
            // Client ID'ye göre pozisyon belirle
            ulong clientId = NetworkManager.Singleton.LocalClientId;

            if (clientId == 1) // Ýlk oyuncu
            {
                localPlayer.transform.SetPositionAndRotation(Player1Transform.position, Player1Transform.rotation);
                Debug.Log("Local Player (Player1) yerleþtirildi.");
            }
            else if (clientId == 2) // Ýkinci oyuncu
            {
                localPlayer.transform.SetPositionAndRotation(Player2Transform.position, Player2Transform.rotation);
                Debug.Log("Local Player (Player2) yerleþtirildi.");
            }
        }
    }
}



