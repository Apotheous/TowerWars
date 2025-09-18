using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// MainScene UI hareketleri baþlangýç ve kapanýþ gibi belki diðer hareketler de buraya çekilebilir.
/// </summary>
public class MainSceneUIManagement : MonoBehaviour
{

    [SerializeField] Canvas MainSceneCanvas;
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
        Debug.Log("MainSceneTrigger: MainScene started canvas opened");
   
        if (MainSceneCanvas != null && !MainSceneCanvas.gameObject.activeSelf)
        {
            MainSceneCanvas.gameObject.SetActive(true);
        }
        
  
        // Buraya bu objeye özel yapýlacak iþleri koy
        // örn: oyun UI’sini aç, player spawn hazýrla, tutorial göster vs.
    }
    private void HandleMainSceneClosed()
    {
        Debug.Log("MainSceneTrigger: MainScene Closed canvas Closed!");
        MainSceneCanvas.gameObject.SetActive(true);
        // Buraya bu objeye özel yapýlacak iþleri koy
        // örn: oyun UI’sini aç, player spawn hazýrla, tutorial göster vs.
    }
}
