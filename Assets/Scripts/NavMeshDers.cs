using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class NavMeshDers : MonoBehaviour
{
    [SerializeField] NavMeshAgent navMesh;
    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
       // navMesh = gameObject.AddComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        navMesh.SetDestination(target.position);
    }
}
