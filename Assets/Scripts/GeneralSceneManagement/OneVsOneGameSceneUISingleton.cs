using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public class OneVsOneGameSceneUISingleton : MonoBehaviour
{
    public static OneVsOneGameSceneUISingleton Instance { get; private set; }


    [SerializeField] private TextMeshProUGUI playerCurrenthHealth;
    [SerializeField] private TextMeshProUGUI myScrapTexter;
    [SerializeField] private TextMeshProUGUI myExpTexter;

    private void Awake()
    {
        // E�er zaten bir Instance varsa ve bu o de�ilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject); // Sahne de�i�ince kaybolmas�n
    }




    public void PlayerCurrentHealthWrite(float health)
    {
        playerCurrenthHealth.text = health.ToString();
    }

    public void PlayerScrapWrite(float scrap)
    {
        myScrapTexter.text = scrap.ToString();
    }
    public void PlayerExpWrite(float exp)
    {
        myExpTexter.text = exp.ToString();
    }



}
