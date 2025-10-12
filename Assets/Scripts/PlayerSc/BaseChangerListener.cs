using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseChangerListener : NetworkBehaviour
{
    [SerializeField] private PlayerSC player;


    public override void OnNetworkSpawn()
    {
        if (IsClient && IsOwner)
        {
            // PlayerSC'nin LocalClient.PlayerObject üzerinde olduğunu varsayarak:
            GameObject localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

            if (localPlayerObject != null)
            {
                player = localPlayerObject.GetComponent<PlayerSC>();
            }


            if (player != null)
            {
                // LevelUp eventine abone ol
                player.OnLevelUp += HandleLevelUp;
            }
            else
            {
                // Artık null değilse, loglama mesajınızı daha bilgilendirici yapın.
                Debug.LogError("HATA: PlayerSC bileşeni yerel oyuncu nesnesinde (LocalPlayerObject) bulunamadı!");
            }
        }

        base.OnNetworkSpawn();
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
                DebugManager.Instance.Log("[BaseChanger] TechPoint 100 oldu →------ LEVEL UP tetiklendi!++++++");

            }
        }
    }
    // Abone olunan eventleri kaldırmak için OnNetworkDespawn() kullanın.
    public override void OnNetworkDespawn()
    {
        if (player != null)
        {
            player.OnLevelUp -= HandleLevelUp;
        }

        base.OnNetworkDespawn();
    }

}


