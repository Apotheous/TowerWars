using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CubeNavMesh : MonoBehaviour
{
    // Inspector'dan atanmas� gereken NavMeshAgent bile�eni
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
            Debug.LogWarning("[SERVER/NavMesh] Yeni hedef null. Atama yap�lmad�.");
            return;
        }

        currentTarget = newTarget;

        if (navMesh != null && navMesh.isOnNavMesh)
        {
            navMesh.ResetPath(); // eski path'i temizle
            navMesh.SetDestination(currentTarget.position); // yeni hedefe y�nlendir
            Debug.Log($"[SERVER/NavMesh] Yeni hedef al�nd�: ({currentTarget.name}). Y�nlendirme ba�lat�ld�.");
        }
        else
        {
            Debug.LogError($"[SERVER/NavMesh] {gameObject.name} NavMesh �zerinde de�il veya Agent bile�eni yok. Hareket ettirilemiyor.");
        }
    }

}
