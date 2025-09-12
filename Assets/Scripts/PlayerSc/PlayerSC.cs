using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerSC : NetworkBehaviour
{
    [SerializeField] float movementSpeedBase = 5;

    // PlayerData'dan gelen health verileri

    [SerializeField] private PlayerGameData playerGameData;


    //private Rigidbody2D rb;
    private float movementSpeedMultiplier;
    private Vector2 currentMoveDirection;
    public int playerScore;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        playerGameData.Initialize(); // NetworkVariable'� ba�lat
        //GeneralUISingleton.Instance.PlayerCurrentHealth(playerGameData.currentHealth.Value);

    }
    private void UpdateMyCurrentHealth(float damage)
    {
        playerGameData.currentHealth.Value += damage;
        GeneralUISingleton.Instance.PlayerCurrentHealth(playerGameData.currentHealth.Value);
    }


    void Update()
    {
        Move();
    }

    private void Move()
    {
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal"); // A, D veya Sol/Sa� ok
        float v = Input.GetAxis("Vertical");   // W, S veya Yukar�/A�a�� ok

        Vector3 move = new Vector3(h, 0f, v) * movementSpeedBase * Time.deltaTime;
        transform.Translate(move, Space.World);
    }



}
[System.Serializable]
public class PlayerGameData
{
    public float initialHealth = 100f;  // edit�rde g�z�kecek
    [HideInInspector] public NetworkVariable<float> currentHealth; // network-only

    public void Initialize()
    {
        currentHealth = new NetworkVariable<float>(initialHealth);
    }
}