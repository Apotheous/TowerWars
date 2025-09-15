using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.AI;


public class NavMeshDers : MonoBehaviour
{
    [SerializeField] private NavMeshAgent navMesh;
    [SerializeField] private Transform target;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        //navMesh = this.gameObject.AddComponent<NavMeshAgent>();
        target = DevSingletonTransform.instance.publicTransform;
        navMesh.destination = target.position;

    }

    // Update is called once per frame
    void Update()
    {
        navMesh.SetDestination(target.position);
    }
}
