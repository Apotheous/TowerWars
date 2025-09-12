using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseChanger : NetworkBehaviour
{
    private PlayerSC player;
    void Start()
    {
        // Sahnedeki PlayerSC’yi bul (ilk bulduğu)
        player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerSC>();

        if (player != null)
        {
            // LevelUp eventine abone ol
            player.OnLevelUp += HandleLevelUp;
        }
        else
        {
            Debug.LogError("PlayerSC ya da PlayerGameData bulunamadı!");
        }
    }


    private void HandleLevelUp()
    {
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayer != null)
        {
            // Client ID'ye göre pozisyon belirle
            ulong clientId = NetworkManager.Singleton.LocalClientId;

            if (clientId == 1) // İlk oyuncu
            {
                DebugManager.Instance.Log2("[BaseChanger] TechPoint 100 oldu →------ LEVEL UP tetiklendi!--------");

            }
            else if (clientId == 2) // İkinci oyuncu
            {
                DebugManager.Instance.Log3("[BaseChanger] TechPoint 100 oldu →------ LEVEL UP tetiklendi!++++++");

            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }


}


