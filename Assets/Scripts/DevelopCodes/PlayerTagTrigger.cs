using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTagTrigger : MonoBehaviour
{
    [SerializeField] private GameObject turretPos1;
    [SerializeField] private GameObject turretPos2;
    [SerializeField] private GameObject turretPos3;
   
    void OnTriggerEnter(Collider other)
    {
        //other.tag = playerTag;
        //gameObject.GetComponent<Collider>().enabled = false;

        gameObject.tag =other.gameObject.tag;
        other.gameObject.GetComponent<Collider>().enabled = false;

        turretPos1.tag = gameObject.tag;
        turretPos2.tag = gameObject.tag;
        turretPos3.tag = gameObject.tag;
    }
}
