using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Counter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private float seconds = 0f;
    private Coroutine counterCoroutine;

    // Coroutine: belirli aral�klarla zaman� art�r
    private IEnumerator StartCounting(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            seconds += waitTime;
            TimerTextWrite();
        }
    }

    // Saya� ba�lat
    public void StartCount(float waitTime = 1f)
    {
        if (counterCoroutine == null)
        {
            seconds = 0f;
            counterCoroutine = StartCoroutine(StartCounting(waitTime));
        }
    }

    // Saya� durdur
    public void StopCount()
    {
        if (counterCoroutine != null)
        {
            StopCoroutine(counterCoroutine);
            counterCoroutine = null;
        }
    }

    // Text�i g�ncelle
    private void TimerTextWrite()
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);

        timerText.text = $"{minutes}:{secs:00}";
        // �rn: 2:05 ? 2 dakika 5 saniye
    }

}
