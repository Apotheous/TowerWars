using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTagHolder : MonoBehaviour
{
    [SerializeField] private string playerTag;
    

    void OnTriggerEnter(Collider other)
    {
            //other.tag = playerTag;
            //gameObject.GetComponent<Collider>().enabled = false;
    }
}
