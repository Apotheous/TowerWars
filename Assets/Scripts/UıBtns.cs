using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UıBtns : MonoBehaviour
{

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host başlatıldı.");
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client bağlanmaya çalışıyor...");
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server başlatıldı.");
    }
}
