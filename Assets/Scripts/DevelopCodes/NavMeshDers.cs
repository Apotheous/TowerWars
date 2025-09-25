using UnityEngine;
using UnityEngine.AI;


public class NavMeshDers : MonoBehaviour
{
    [SerializeField] private NavMeshAgent navMesh;
    [SerializeField] private Transform target;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        if (DevSingletonTransform.instance.publicTransform!=null)
        {
            //navMesh = this.gameObject.AddComponent<NavMeshAgent>();
            target = DevSingletonTransform.instance.publicTransform;
            navMesh.destination = target.position;
        }


    }

    // Update is called once per frame
    void Update()
    {
        if(target!= null)
        navMesh.SetDestination(target.position);
    }
}
