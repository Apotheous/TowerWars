using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI timeText;

    private float deltaTime = 0.0f; // FPS hesaplamasý için

    void Update()
    {
        // Her karede FPS ve Süreyi güncelle
        UpdateFPSCounter();
        UpdateTimeCounter();
    }

    /// <summary>
    /// FPS deðerini hesaplar ve UI'ya yazar.
    /// </summary>
    private void UpdateFPSCounter()
    {
        if (fpsText == null) return;

        // DeltaTime hesapla (yumuþatýlmýþ ortalama)
        // Time.unscaledDeltaTime kullanmak, oyun durakladýðýnda FPS'in etkilenmemesini saðlar.
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // FPS deðerini hesapla
        float fps = 1.0f / deltaTime;

        // UI'ya yazdýr (tam sayýya yuvarlanmýþ)
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }

    /// <summary>
    /// Oyunun baþlangýcýndan itibaren geçen süreyi hesaplar ve UI'ya yazar (MM:SS).
    /// </summary>
    private void UpdateTimeCounter()
    {
        if (timeText == null) return;

        // Oyunun toplam çalýþma süresi (saniye cinsinden)
        float totalTimeSeconds = Time.time;

        // Dakika ve saniyeyi hesapla
        int minutes = Mathf.FloorToInt(totalTimeSeconds / 60F);
        int seconds = Mathf.FloorToInt(totalTimeSeconds % 60F);

        // UI'ya "MM:SS" formatýnda yazdýr
        // D2 formatý, tek haneli sayýlarýn baþýna sýfýr ekler (örn: 05)
        timeText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds+"Time");
    }
}
