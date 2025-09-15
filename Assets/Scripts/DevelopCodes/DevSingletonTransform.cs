using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevSingletonTransform : MonoBehaviour
{
    public static DevSingletonTransform instance;
    public Transform player1Transform, player2Transform,publicTransform;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Sahne ge�i�lerinde yok olmas�n
        }
        else
        {
            Destroy(gameObject); // �ift olu�umu engelle
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
