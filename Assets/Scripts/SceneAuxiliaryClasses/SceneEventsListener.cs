using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEventsListener : MonoBehaviour
{
    //private void OnEnable()
    //{
    //    if (NetworkManager.Singleton != null)
    //    {
    //        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
    //    }
    //}

    //private void OnDisable()
    //{
    //    if (NetworkManager.Singleton != null)
    //    {
    //        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
    //    }
    //}

    //private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    //{
    //    Debug.Log($"Client {clientId} finished loading scene: {sceneName}");
    //}
}
