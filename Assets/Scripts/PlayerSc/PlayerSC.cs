using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Player_Game_Mode_Manager;
using NFloat = Unity.Netcode.NetworkVariable<float>;

public class PlayerSC : NetworkBehaviour ,IDamageable
{


    #region Player Data
    [Header("Player Stats")]
    // NetworkVariable olarak Player Data’yı direkt burada tanımlıyoruz
    public NetworkVariable<float> initalHealth = new NetworkVariable<float>();

    // NetworkVariable olarak Player Data’yı direkt burada tanımlıyoruz
    public NetworkVariable<float> mycurrentHealth = new NetworkVariable<float>();


    //public NFloat currentHealth2 = new NFloat(100f);
    public NetworkVariable<float> myExpPoint = new NetworkVariable<float>();



    public NetworkVariable<float> myCurrentScrap = new NetworkVariable<float>();
    // Custom eventler
    public event Action<float, float> OnScrapChanged;



    // Custom eventler
    public event Action<float, float> OnTechPointChanged;
    public event Action OnLevelUp;

    private const float LEVEL_UP_THRESHOLD = 100f;


    #endregion


    [Header("Movement Settings")]
    [SerializeField] float movementSpeedBase = 5f;

    [SerializeField] Transform myweapon;
    [SerializeField] GameObject bulletPrefab;
    public override void OnNetworkSpawn()
    {
        //Stat abonelikleri
        //canı değişiklik olduğunda ötecek sisteme bağlamak
        mycurrentHealth.OnValueChanged += OnHealthChanged;


        myCurrentScrap.OnValueChanged += OnMyScrapChanged;


        // NetworkVariable değişimlerini dinle
        myExpPoint.OnValueChanged += OnExpPointChanged;


        // Debug için event subscribe (isteğe bağlı)
        OnTechPointChanged += (oldValue, newValue) =>
        {
            Debug.Log($"[PlayerSC] TechPoint changed: {oldValue} → {newValue}");
        };

        OnLevelUp += () =>
        {
            Debug.Log("[PlayerSC] LEVEL UP TRIGGERED!");
        };
    }

    #region Player Stats abonelikler
    private void OnHealthChanged(float previous, float current)
    {
        if (IsOwner)
        {
            OneVsOneGameSceneUISingleton.Instance.PlayerCurrentHealthWrite(current);
        }
    }
    private void OnMyScrapChanged(float previous, float current)
    {
        if (IsOwner)
        {
            OneVsOneGameSceneUISingleton.Instance.PlayerScrapWrite(current);
        }
    }
    private void OnExpPointChanged(float previous, float current)
    {
        if (IsOwner)
        {
            OneVsOneGameSceneUISingleton.Instance.PlayerExpWrite(current);
        }
    }




    #endregion


    private void Update()
    {
        if (!IsOwner) return;
        //Move();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireBulletServerRpc();
        }
    }








    #region Movement and fire Yalandan


    [ServerRpc]
    private void FireBulletServerRpc()
    {
        // Server authoritative spawn
        GameObject bullet = Instantiate(bulletPrefab, myweapon.position, myweapon.rotation);
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    //private void Move()
    //{


    //    float h = Input.GetAxis("Horizontal"); // A, D veya Sol/Sağ ok
    //    float v = Input.GetAxis("Vertical");   // W, S veya Yukarı/Aşağı ok

    //    Vector3 move = new Vector3(h, 0f, v) * movementSpeedBase * Time.deltaTime;
    //    transform.Translate(move, Space.World);
    //}
    #endregion



    #region Health Logic

    /// <summary>
    /// Playerın currentHealth değerini değiştirir
    /// </summary>
    /// <param name="damage"></param>
    public void UpdateMyCurrentHealth(float damage)
    {
        if (IsServer)
        {
            mycurrentHealth.Value += damage;
            if (mycurrentHealth.Value <= 0)
            {
                Die();
            }
        }
        else
        {
            RequestUpdateHealthServerRpc(damage);
        }
    }

    [ServerRpc]
    private void RequestUpdateHealthServerRpc(float damage)
    {
        mycurrentHealth.Value += damage;
    }


    public void TakeDamage(float damageAmount)
    {
        UpdateMyCurrentHealth(damageAmount);
    }

    public void Die()
    {
        // Ölüm işlemleri burada yapılır
        Debug.Log("Player died!");
    }
    #endregion


    #region Scrap Logic

    /// <summary>
    /// Playerın scrap değerini değiştirir
    /// </summary>
    /// <param name="damage"></param>
    public void UpdateMyScrap(float amount)
    {
        if (IsServer)
        {
            myCurrentScrap.Value += amount;
        }
        else
        {
            RequestUpdateScrapServerRpc(amount);
        }
    }

    [ServerRpc]
    private void RequestUpdateScrapServerRpc(float amount)
    {
        myCurrentScrap.Value += amount;
    }
    #endregion


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

    public void UpdateExpPointIncrease(float amount)
    {
        if (IsServer)
        {
            // Server authoritative → direkt değiştir
            myExpPoint.Value += amount;
        }
        else
        {
            // Client → ServerRpc üzerinden artır
            RequestExpPointsServerRpc(amount);
        }
    }


    [ServerRpc]
    private void RequestExpPointsServerRpc(float amount)
    {
        myExpPoint.Value += amount;
    }

  


    #endregion
}
