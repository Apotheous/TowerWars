using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DebugManager : NetworkBehaviour
{
    public static DebugManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI text1;
    [SerializeField] private TextMeshProUGUI Text2Ýnt;
    [SerializeField] private TextMeshProUGUI text3;

    [SerializeField] private NetworkSpawnManager spawnManager;

    private void Awake()
    {
        // Eðer zaten bir Instance varsa ve bu o deðilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Sahne deðiþince kaybolmasýn
    }

    private void Start()
    {

  
    }


    private void Update()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            text1.text = " Baðlandý ="+NetworkManager.Singleton.ConnectedClientsIds.Count;
        }
        else { text1.text = " Baðlanýlýyor"; }
        //onlinePlayerCountText.text = NetworkManager.Singleton.ConnectedClientsIds.Count.ToString();
        //text3.text = NetworkManager.Singleton.ServerTime.ToString() ;
    }
    public void DenemeBtn()
    {
        //NetworkManager.Singleton.Shutdown();
        if (NetworkManager.Singleton == null)
        {
            Text2Ýnt.text = "NetworkManager yok";
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            // Server'da gerçek sayý
            int count = NetworkManager.Singleton.ConnectedClientsList.Count;
            Text2Ýnt.text = $"Online: {count}";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // Client'ta sadece baðlantý durumu
            Text2Ýnt.text = "Client - Baðlý Count = " + NetworkManager.Singleton.ConnectedClientsList.Count.ToString();
        }
        else
        {
            // Baðlantý yok
            Text2Ýnt.text = "Offline";
        }

    }
    /// <summary>
    /// Debug mesajý ekrana basar.
    /// </summary>
    public void Log(string message)
    {
        Debug.Log(message); // Unity Console’a da yaz
        if (text1 != null)
        {
            text1.text += $"\n{message}";
        }
    }
    public void Log2(string message)
    {
        Debug.Log(message); // Unity Console’a da yaz
        if (text1 != null)
        {
            text1.text += $"\n{message}";
        }
    }
    public void Log3(string message)
    {
        Debug.Log(message); // Unity Console’a da yaz
        if (text1 != null)
        {
            text1.text += $"\n{message}";
        }
    }


}
