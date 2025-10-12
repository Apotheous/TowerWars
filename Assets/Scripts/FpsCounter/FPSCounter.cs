using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI timeText;

    private float deltaTime = 0.0f; // FPS hesaplamas� i�in

    void Update()
    {
        // Her karede FPS ve S�reyi g�ncelle
        UpdateFPSCounter();
        UpdateTimeCounter();
    }

    /// <summary>
    /// FPS de�erini hesaplar ve UI'ya yazar.
    /// </summary>
    private void UpdateFPSCounter()
    {
        if (fpsText == null) return;

        // DeltaTime hesapla (yumu�at�lm�� ortalama)
        // Time.unscaledDeltaTime kullanmak, oyun duraklad���nda FPS'in etkilenmemesini sa�lar.
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // FPS de�erini hesapla
        float fps = 1.0f / deltaTime;

        // UI'ya yazd�r (tam say�ya yuvarlanm��)
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }

    /// <summary>
    /// Oyunun ba�lang�c�ndan itibaren ge�en s�reyi hesaplar ve UI'ya yazar (MM:SS).
    /// </summary>
    private void UpdateTimeCounter()
    {
        if (timeText == null) return;

        // Oyunun toplam �al��ma s�resi (saniye cinsinden)
        float totalTimeSeconds = Time.time;

        // Dakika ve saniyeyi hesapla
        int minutes = Mathf.FloorToInt(totalTimeSeconds / 60F);
        int seconds = Mathf.FloorToInt(totalTimeSeconds % 60F);

        // UI'ya "MM:SS" format�nda yazd�r
        // D2 format�, tek haneli say�lar�n ba��na s�f�r ekler (�rn: 05)
        timeText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds+"Time");
    }
}
