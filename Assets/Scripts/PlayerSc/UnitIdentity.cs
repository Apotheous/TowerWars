using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UnitIdentity : MonoBehaviour
{
    // Bu de�i�ken t�m client'lara senkronize edilecek.
    // ReadPermission.Everyone -> Herkes okuyabilir
    // WritePermission.Server -> Sadece server de�i�tirebilir
    public NetworkVariable<int> TeamId = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
}
