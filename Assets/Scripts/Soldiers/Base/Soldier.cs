using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class Soldier : NetworkBehaviour, IDamageable
{
    public Player_Game_Mode_Manager.PlayerAge age;
    [SerializeField] private float MaxHealth = 100f;



    // Health de�erini a�da senkronize tutuyoruz
    private NetworkVariable<float> myHealth = new NetworkVariable<float>(
        100f,  // varsay�lan de�er
        NetworkVariableReadPermission.Everyone,  // herkes okuyabilir
        NetworkVariableWritePermission.Server    // sadece server yazabilir
    );

    //PlayerProductionManagement ProduceNextUnit() dunda dolduruluyor.

    // Bu de�i�ken t�m client'lara senkronize edilecek.
    // ReadPermission.Everyone -> Herkes okuyabilir
    // WritePermission.Server -> Sadece server de�i�tirebilir
    public NetworkVariable<int> TeamId = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Unity Stuff")]
    public Image healthBar;

    [SerializeField] private float myCost;
    [SerializeField] private float myPrize;
    [SerializeField] private Transform myBarrel;
    [SerializeField] private float myRange;


    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private TargetDetector targetDetector;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable'lar�n senkronizasyonu tamamland���nda bu metot �al���r.

        // 2. Sadece Server �zerinde �al��acak mant�k
        if (IsServer)
        {
            myHealth.Value = MaxHealth;

            gameObject.name = TeamId.Value.ToString();

            // �rne�in, burada tak�m�na g�re bir hedef bulma mant��� �al��t�r�labilir.
            // FindInitialTarget(); 


            // 1. TeamId'yi G�VENL� bir �ekilde al ve sakla
            var soldiersControllerNavMesh = GetComponent<SoldiersControllerNavMesh>();
            if (soldiersControllerNavMesh != null)
            {
                // Coroutine'i bu �ekilde ba�latmal�s�n:
                StartCoroutine(soldiersControllerNavMesh.FindTargetAndSetDestination());
            }
            else
            {
                Debug.LogError("Bu objenin �zerinde SoldiersControllerNavMesh component'i bulunamad�!", gameObject);
            }
        }
        // 3. T�m client'larda �al��acak mant�k (G�rsel g�ncellemeler vb.)
        myHealth.OnValueChanged += OnHealthChanged;
        OnHealthChanged(0, myHealth.Value); // UI'� ilk de�erle g�ncelle
        if (targetDetector != null)
        {
            targetDetector.WhenNetworkSpawn();
        }
        else
        {
            Debug.LogError("Bu objenin �zerinde TargetDetector component'i bulunamad�!", gameObject);
        }

    }


    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthBar != null)
            healthBar.fillAmount = newValue / MaxHealth;
    }



    // === IDamageable implementasyonu ===
    [ServerRpc]
    public void TakeDamageServerRpc(float damageAmount)
    {
        TakeDamage(damageAmount); // interface metodunu �a��r
    }

    public void TakeDamage(float damageAmount)
    {
     
        if (myHealth.Value <= 0) return;

        myHealth.Value -= damageAmount;
        if (myHealth.Value <= 0)
        {
            Die();
        }
       
    }

    public void Die()
    {
        gameObject.SetActive(false);
        // GetComponent<NetworkObject>().Despawn(); // alternatifi
    }



}
