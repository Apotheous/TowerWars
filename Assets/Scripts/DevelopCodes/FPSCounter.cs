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
        // DeltaTime hesapla (FPS i�in tersini alaca��z)
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

        // FPS de�erini hesapla
        float fps = 1.0f / deltaTime;

        // UI'ya yazd�r
        if (fpsText != null)
        {
            fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
        }
    }
}
