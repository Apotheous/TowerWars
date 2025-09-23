using System;
using Unity.Netcode;
using UnityEngine;

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

    public NetworkVariable<ulong> WinnerClientId = new NetworkVariable<ulong>(0);

    public override void OnNetworkSpawn()
    {
        //Stat abonelikleri
        //canı değişiklik olduğunda ötecek sisteme bağlamak
        mycurrentHealth.OnValueChanged += OnHealthChanged;


        myCurrentScrap.OnValueChanged += OnMyScrapChanged;


        // NetworkVariable değişimlerini dinle
        myExpPoint.OnValueChanged += OnExpPointChanged;

        // WinnerClientId değişimini özel olarak ele al
        WinnerClientId.OnValueChanged += OnWinnerDeclared;

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

    private void OnWinnerDeclared(ulong previousValue, ulong newValue)
    {
        if (newValue != 0) // 0 = kimse kazanmadı varsayalım
        {
            if (IsOwner) // sadece kendi ekranımda göster
            {
                bool benKazandim = (newValue == NetworkManager.Singleton.LocalClientId);
                OneVsOneGameSceneUISingleton.Instance.ShowGameOver("Oyun bitti!", benKazandim);
            }
        }
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
        if (IsServer)
        {
            Debug.Log($"Player {OwnerClientId} died (server).");
            OneVsOneGameManager.Instance.HandlePlayerDeath(OwnerClientId);
            // opsiyonel: oyuncu objesini devre dışı bırak
            RpcDisablePlayerVisualsClientRpc();
        }
    }

    [ClientRpc]
    private void RpcDisablePlayerVisualsClientRpc()
    {
        // görselleri/sesleri kapat; kontrol scriptlerini disable et
        var inputComp = GetComponent<PlayerSC>(); // senin input component'ine göre değiştir
        if (inputComp != null && IsOwner) inputComp.enabled = false;
        // animasyon, collider vb. kapat
        // gameObject.SetActive(false); -> dikkat, NetObject de unmanaged olabilir
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
