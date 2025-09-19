using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
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

    private void OnEnable()
    {
        // Sa�l�k server�da initialize edilir
        if (IsServer)
        {
            myHealth.Value = MaxHealth;
        }

        // Health de�i�ti�inde UI g�ncellensin
        myHealth.OnValueChanged += OnHealthChanged;

        // �lk UI g�ncellemesi
        OnHealthChanged(0, myHealth.Value);

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
