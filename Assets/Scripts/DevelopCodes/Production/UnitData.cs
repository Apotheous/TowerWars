using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[CreateAssetMenu(menuName = "Game/UnitData")]
public class UnitData : ScriptableObject
{
    public string id;
    public Player_Game_Mode_Manager.PlayerAge age; // Hangi çað için geçerli
    public float trainingTime;
    public int cost;
    public GameObject prefab;

    [Header("UI")]
    public Sprite icon; // Askerin UI’daki resmi
}