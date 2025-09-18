using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainSceneListener : MonoBehaviour
{
    private void OnEnable()
    {
        // Event’e abone ol
        MainSceneManager.OnMainSceneStarted += HandleMainSceneStarted;
        MainSceneManager.OnMainSceneClosed += HandleMainSceneClosed;
    }

    private void OnDisable()
    {
        // Event’ten çýk (önemli! yoksa memory leak olabilir)
        MainSceneManager.OnMainSceneStarted -= HandleMainSceneStarted;
        MainSceneManager.OnMainSceneClosed -= HandleMainSceneClosed;
    }

    private void HandleMainSceneStarted()
    {
        Debug.Log("MainSceneTrigger: MainScene started event received!");

        // Buraya bu objeye özel yapýlacak iþleri koy
        // örn: oyun UI’sini aç, player spawn hazýrla, tutorial göster vs.
    }
    private void HandleMainSceneClosed()
    {
        Debug.Log("MainSceneTrigger: MainScene Closed event received!");

        // Buraya bu objeye özel yapýlacak iþleri koy
        // örn: oyun UI’sini aç, player spawn hazýrla, tutorial göster vs.
    }
}
