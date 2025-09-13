using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public class GeneralUISingleton : MonoBehaviour
{
    public static GeneralUISingleton Instance { get; private set; }


    [SerializeField] private TextMeshProUGUI playerCurrenthHealth;

    private void Awake()
    {
        // Eðer zaten bir Instance varsa ve bu o deðilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject); // Sahne deðiþince kaybolmasýn
    }




    public void PlayerCurrentHealth(float health)
    {
        playerCurrenthHealth.text = health.ToString();
    }
}
