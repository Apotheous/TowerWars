using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainSceneListener : MonoBehaviour
{
    private void OnEnable()
    {
        // Event�e abone ol
        MainSceneManager.OnMainSceneStarted += HandleMainSceneStarted;
        MainSceneManager.OnMainSceneClosed += HandleMainSceneClosed;
    }

    private void OnDisable()
    {
        // Event�ten ��k (�nemli! yoksa memory leak olabilir)
        MainSceneManager.OnMainSceneStarted -= HandleMainSceneStarted;
        MainSceneManager.OnMainSceneClosed -= HandleMainSceneClosed;
    }

    private void HandleMainSceneStarted()
    {
        Debug.Log("MainSceneTrigger: MainScene started event received!");

        // Buraya bu objeye �zel yap�lacak i�leri koy
        // �rn: oyun UI�sini a�, player spawn haz�rla, tutorial g�ster vs.
    }
    private void HandleMainSceneClosed()
    {
        Debug.Log("MainSceneTrigger: MainScene Closed event received!");

        // Buraya bu objeye �zel yap�lacak i�leri koy
        // �rn: oyun UI�sini a�, player spawn haz�rla, tutorial g�ster vs.
    }
}
