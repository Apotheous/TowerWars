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
    [SerializeField] private TextMeshProUGUI forUpdateText;

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
        //DontDestroyOnLoad(gameObject); // Sahne deðiþince kaybolmasýn
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
    /// <summary>
    /// Buraya Yazfalan
    /// </summary>
  
    public void Log2(string message)
    {
        Debug.Log(message); // Unity Console’a da yaz
        if (text1 != null)
        {
            Text2Ýnt.text += $"\n{message}";
        }
    }

    
    public void Log3(string message)
    {
        Debug.Log(message); // Unity Console’a da yaz
        if (text3 != null)
        {
            text3.text += $"\n{message}";
        }
    }
    public void ForUpdate(string message)
    {
        Debug.Log(message); // Unity Console’a da yaz
        if (text1 != null)
        {
            forUpdateText.text += $"\n{message}";
        }
    }


}
