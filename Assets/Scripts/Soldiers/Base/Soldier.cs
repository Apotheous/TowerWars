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

 

    [Header("Unity Stuff")]
    public Image healthBar;

    [SerializeField] private float myCost;
    [SerializeField] private float myPrize;
    [SerializeField] private Transform myBarrel;
    [SerializeField] private float myRange;

    // Tak�m ID'sini bu de�i�kende saklayaca��z.
    private int myTeamId = -1;

    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private TargetDetector targetDetector;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable'lar�n senkronizasyonu tamamland���nda bu metot �al���r.

        // 1. TeamId'yi G�VENL� bir �ekilde al ve sakla
        var unitIdentity = GetComponent<UnitIdentity>();
        if (unitIdentity != null)
        {
            myTeamId = unitIdentity.TeamId.Value;
            
        }
        else
        {
            Debug.LogError("Bu objenin �zerinde UnitIdentity component'i bulunamad�!", gameObject);
        }


        // 2. Sadece Server �zerinde �al��acak mant�k
        if (IsServer)
        {
            myHealth.Value = MaxHealth;
            Debug.Log($"[SERVER] Asker spawn oldu. Tak�m�: {myTeamId}", gameObject);
            gameObject.name = myTeamId.ToString();

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


    // Soldier.cs i�ine bu metodu ekle
    public int GetTeamId()
    {
        return myTeamId;
    }
}
