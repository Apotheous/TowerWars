using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// MainScene UI hareketleri ba�lang�� ve kapan�� gibi belki di�er hareketler de buraya �ekilebilir.
/// </summary>
public class MainSceneUIManagement : MonoBehaviour
{

    [SerializeField] Canvas MainSceneCanvas;
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
        Debug.Log("MainSceneTrigger: MainScene started canvas opened");
   
        if (MainSceneCanvas != null && !MainSceneCanvas.gameObject.activeSelf)
        {
            MainSceneCanvas.gameObject.SetActive(true);
        }
        
  
        // Buraya bu objeye �zel yap�lacak i�leri koy
        // �rn: oyun UI�sini a�, player spawn haz�rla, tutorial g�ster vs.
    }
    private void HandleMainSceneClosed()
    {
        Debug.Log("MainSceneTrigger: MainScene Closed canvas Closed!");
        MainSceneCanvas.gameObject.SetActive(true);
        // Buraya bu objeye �zel yap�lacak i�leri koy
        // �rn: oyun UI�sini a�, player spawn haz�rla, tutorial g�ster vs.
    }
}
