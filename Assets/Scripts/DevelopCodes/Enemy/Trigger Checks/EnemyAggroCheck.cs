using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    public GameObject PlayerTarget {  get; set; }
    private Enemy _enemy;

    private void Awake()
    {
        //PlayerTarget = GameObject.FindWithTag("Player");
        _enemy = GetComponent<Enemy>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == PlayerTarget)
        {
            Debug.Log("EnemyAgroCheck");
           // _enemy.SetAggroStatus(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == PlayerTarget)
        {
           //_enemy.SetAggroStatus(false);
        }
    }
}
