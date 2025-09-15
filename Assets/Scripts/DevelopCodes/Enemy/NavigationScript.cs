using UnityEngine;
using UnityEngine.AI;


public class NavigationScript : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.destination = player.position;
    }

}
