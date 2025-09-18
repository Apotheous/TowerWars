using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{
    
    public void LoadGameScene(string sceneName)
    {
        if (NetworkManager.Singleton.IsServer) // sadece server/host sahneyi yükler
        {
            Debug.Log($"Loading scene: {sceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("Only server/host can change scenes!");
        }
    }
}
