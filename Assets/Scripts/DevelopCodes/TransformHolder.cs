using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TransformHolder : MonoBehaviour
{
    public NavMeshAgent navMeshAgent;
    public Transform targetTransform1;
    public Transform targetTransform2;
    public Transform targetTransform3;
    public Transform targetTransform4;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CallTransforn1();
    }

    public void CallTransforn1()
    {
        navMeshAgent.SetDestination(targetTransform1.position);
    }
    public void CallTransforn2()
    {
        navMeshAgent.SetDestination(targetTransform2.position);
    }
    public void CallTransforn3()
    {
            navMeshAgent.SetDestination(targetTransform3.position);
    }
    public void CallTransforn4()
    {
        navMeshAgent.SetDestination(targetTransform4.position);
    }
}
