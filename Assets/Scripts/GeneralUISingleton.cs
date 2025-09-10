using System.Collections;
using System.Collections.Generic;
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
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayerCurrentHealth(float health)
    {
        playerCurrenthHealth.text = health.ToString();
    }
}
