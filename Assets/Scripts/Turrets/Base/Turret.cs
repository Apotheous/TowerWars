using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Turret : NetworkBehaviour
{
    public Player_Game_Mode_Manager.PlayerAge age;



    // Bu de�i�ken t�m client'lara senkronize edilecek.
    // ReadPermission.Everyone -> Herkes okuyabilir
    // WritePermission.Server -> Sadece server de�i�tirebilir
    public NetworkVariable<int> TeamId = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    [SerializeField] private float myCost;
    [SerializeField] private float myPrizeScrap;
    [SerializeField] private float myPrizeExp;

    [Header("My Comps")]
    private TurretsRotationController soldiersControllerNavMesh;
    [SerializeField] private TargetDetector targetDetector;
    private void OnEnable()
    {





    }







}
