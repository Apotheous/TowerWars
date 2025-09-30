using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Game/TurretData")]
public class TurretData : ScriptableObject
{
    public string id;
    public Player_Game_Mode_Manager.PlayerAge age; // Hangi çað için geçerli
    public float trainingTime;
    public float cost;
    public GameObject prefab;

    [Header("UI")]
    public Sprite icon; // Askerin UI’daki resmi
}
