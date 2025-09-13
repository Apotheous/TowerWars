using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static PlayerScene_and_Game_Mode_Changer;
//using NFloat = Unity.Netcode.NetworkVariable<float>;

public class PlayerSC : NetworkBehaviour
{

    [Header("Movement Settings")]
    [SerializeField] float movementSpeedBase = 5f;

    #region Player Data
    [Header("Player Stats")]

    // NetworkVariable olarak Player Data’yı direkt burada tanımlıyoruz
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    //public NFloat currentHealth = new NFloat(100f);
    public NetworkVariable<float> TechPoint = new NetworkVariable<float>(0f);

    // Custom eventler
    public event Action<float, float> OnTechPointChanged;
    public event Action OnLevelUp;

    private const float LEVEL_UP_THRESHOLD = 100f;

    #endregion

    [SerializeField] Transform myweapon;
    [SerializeField] GameObject bulletPrefab;
    public override void OnNetworkSpawn()
    {
        // Sadece owner input ve gain işlemleri yapacak
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        currentHealth.OnValueChanged += HandleHealthChanged;

      

        // NetworkVariable değişimlerini dinle
        TechPoint.OnValueChanged += HandleTechPointChanged;

        // Debug için event subscribe (isteğe bağlı)
        OnTechPointChanged += (oldValue, newValue) =>
        {
            Debug.Log($"[PlayerSC] TechPoint changed: {oldValue} → {newValue}");
        };

        OnLevelUp += () =>
        {
            Debug.Log("[PlayerSC] LEVEL UP TRIGGERED!");
        };

        // Owner kendi puanını artırabilir
        InvokeRepeating(nameof(GainTechPoints), 2f, 2f);
    }

    private void HandleHealthChanged(float oldValue, float newValue)
    {
        if (IsOwner)
        {
            GeneralUISingleton.Instance.PlayerCurrentHealth(newValue);
        }
    }

    #region TechPoint Logic
    private void HandleTechPointChanged(float oldValue, float newValue)
    {
        // Event forward
        OnTechPointChanged?.Invoke(oldValue, newValue);

        // Level-up kontrolü
        if (newValue >= LEVEL_UP_THRESHOLD && oldValue < LEVEL_UP_THRESHOLD)
        {
            Debug.Log("Player leveled up!");
            OnLevelUp?.Invoke();
        }
    }

    private void GainTechPoints()
    {
        if (IsServer)
        {
            // Server authoritative → direkt değiştir
            TechPoint.Value += 25f;
        }
        else
        {
            // Client → ServerRpc üzerinden artır
            RequestGainTechPointsServerRpc(25f);
        }
    }

    [ServerRpc]
    private void RequestGainTechPointsServerRpc(float amount)
    {
        TechPoint.Value += amount;
    }
    #endregion

    #region Health Logic
    public void UpdateMyCurrentHealth(float damage)
    {
        if (IsServer)
        {
            currentHealth.Value += damage;
        }
        else
        {
            RequestUpdateHealthServerRpc(damage);
        }

        // UI güncellemesi
        GeneralUISingleton.Instance.PlayerCurrentHealth(currentHealth.Value);
    }

    [ServerRpc]
    private void RequestUpdateHealthServerRpc(float damage)
    {
        currentHealth.Value += damage;
    }
    #endregion

    [ServerRpc]
    private void FireBulletServerRpc()
    {
        // Server authoritative spawn
        GameObject bullet = Instantiate(bulletPrefab, myweapon.position, myweapon.rotation);
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    #region Movement
    private void Update()
    {
        if (!IsOwner) return;
        Move();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireBulletServerRpc();
        }
    }

    private void Move()
    {


        float h = Input.GetAxis("Horizontal"); // A, D veya Sol/Sağ ok
        float v = Input.GetAxis("Vertical");   // W, S veya Yukarı/Aşağı ok

        Vector3 move = new Vector3(h, 0f, v) * movementSpeedBase * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
    #endregion


}
