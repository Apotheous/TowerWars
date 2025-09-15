using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStrikingDistanceCheck : MonoBehaviour
{
    public GameObject PlayerTarget {  get; set; }
    private Enemy _enemy;
    private void Awake()
    {
        PlayerTarget = GameObject.FindWithTag("Player");
        _enemy = GetComponent<Enemy>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == PlayerTarget)
        {
            Debug.Log("EnemyStrikking");
            _enemy.SetStrikingDistanceBool(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == PlayerTarget)
        {
            _enemy.SetStrikingDistanceBool(false);
        }
    }
}
