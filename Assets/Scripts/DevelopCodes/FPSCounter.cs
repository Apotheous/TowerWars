using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    void Update()
    {
        // DeltaTime hesapla (FPS için tersini alacaðýz)
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

        // FPS deðerini hesapla
        float fps = 1.0f / deltaTime;

        // UI'ya yazdýr
        if (fpsText != null)
        {
            fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
        }
    }
}
