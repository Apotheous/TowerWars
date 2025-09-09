using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    public NetworkVariable<int> myInitialHealth = new NetworkVariable<int>(100);
    public NetworkVariable<int> myCurrentHealth = new NetworkVariable<int>(100);


    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            myInitialHealth.Value = myInitialHealth.Value;
            Debug.Log($"My Health : {myCurrentHealth.Value}");
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
