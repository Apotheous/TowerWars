using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CubeNavMesh : MonoBehaviour
{
    // Inspector'dan atanmasý gereken NavMeshAgent bileþeni
    [SerializeField] private NavMeshAgent navMesh;
    [SerializeField] private Transform baseTarget;
    [SerializeField] private Transform currentTarget;
    void Start()
    {
        navMesh = GetComponent<NavMeshAgent>();
        if (currentTarget != null)
        {
            GiveMeNewTarget(currentTarget);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GiveMeNewTarget(Transform newTarget)
    {

        if (newTarget == null)
        {
            Debug.LogWarning("[SERVER/NavMesh] Yeni hedef null. Atama yapýlmadý.");
            return;
        }

        currentTarget = newTarget;

        if (navMesh != null && navMesh.isOnNavMesh)
        {
            navMesh.ResetPath(); // eski path'i temizle
            navMesh.SetDestination(currentTarget.position); // yeni hedefe yönlendir
            Debug.Log($"[SERVER/NavMesh] Yeni hedef alýndý: ({currentTarget.name}). Yönlendirme baþlatýldý.");
        }
        else
        {
            Debug.LogError($"[SERVER/NavMesh] {gameObject.name} NavMesh üzerinde deðil veya Agent bileþeni yok. Hareket ettirilemiyor.");
        }
    }

}
