using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DebugManager : NetworkBehaviour
{
    public static DebugManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI text1;
    [SerializeField] private TextMeshProUGUI Text2�nt;
    [SerializeField] private TextMeshProUGUI text3;

    [SerializeField] private NetworkSpawnManager spawnManager;

    private void Awake()
    {
        // E�er zaten bir Instance varsa ve bu o de�ilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Sahne de�i�ince kaybolmas�n
    }

    private void Start()
    {

  
    }


    private void Update()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            text1.text = " Ba�land� ="+NetworkManager.Singleton.ConnectedClientsIds.Count;
        }
        else { text1.text = " Ba�lan�l�yor"; }
        //onlinePlayerCountText.text = NetworkManager.Singleton.ConnectedClientsIds.Count.ToString();
        //text3.text = NetworkManager.Singleton.ServerTime.ToString() ;
    }
    public void DenemeBtn()
    {
        //NetworkManager.Singleton.Shutdown();
        if (NetworkManager.Singleton == null)
        {
            Text2�nt.text = "NetworkManager yok";
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            // Server'da ger�ek say�
            int count = NetworkManager.Singleton.ConnectedClientsList.Count;
            Text2�nt.text = $"Online: {count}";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // Client'ta sadece ba�lant� durumu
            Text2�nt.text = "Client - Ba�l� Count = " + NetworkManager.Singleton.ConnectedClientsList.Count.ToString();
        }
        else
        {
            // Ba�lant� yok
            Text2�nt.text = "Offline";
        }

    }
    /// <summary>
    /// Debug mesaj� ekrana basar.
    /// </summary>
    public void Log(string message)
    {
        Debug.Log(message); // Unity Console�a da yaz
        if (text1 != null)
        {
            text1.text += $"\n{message}";
        }
    }
    public void Log2(string message)
    {
        Debug.Log(message); // Unity Console�a da yaz
        if (text1 != null)
        {
            text1.text += $"\n{message}";
        }
    }
    public void Log3(string message)
    {
        Debug.Log(message); // Unity Console�a da yaz
        if (text1 != null)
        {
            text1.text += $"\n{message}";
        }
    }


}
