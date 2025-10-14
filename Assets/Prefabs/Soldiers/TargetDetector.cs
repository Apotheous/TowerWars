using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    private SoldiersControllerNavMesh controllerNavMesh;
    private SoldiersAttackController controllerAttack;
    private Soldier soldier; // Kendi tak�m bilgimizi tutan referans


    // Potansiyel d��manlar� tutan liste
    private List<Transform> detectedTargets = new List<Transform>();

    public void WhenNetworkSpawn()
    {
        //// Yaln�zca Sunucuda gerekli bile�enleri �ekme
        //if (!IsServer) return;
        Debug.Log($"[SERVER DETECTOR] Ba�lad� OnNetworkSpawn");
        // SoldiersControllerNavMesh'e ula�man�n en sa�lam yolu:
        if (controllerNavMesh == null)
        {
            controllerNavMesh = GetComponentInParent<SoldiersControllerNavMesh>();
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh �ekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh zaten atanm��");
        }
        if (controllerAttack == null)
        {
            controllerAttack = GetComponentInParent<SoldiersAttackController>();
            Debug.Log($"[SERVER DETECTOR] SoldiersAttackController �ekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersAttackController zaten atanm��");
        }
        // Kendi UnitIdentity'mizi bul (tak�m bilgisi i�in kritik).
        if (soldier == null)
        {
            soldier = GetComponentInParent<Soldier>();
            if (soldier != null)
            {
                Debug.Log($"[SERVER DETECTOR] Soldier �ekildi MyTeamID ===" + soldier.TeamId.Value + "++++");
            }
            else
            {
                Debug.LogError("TargetDetector i�in gerekli olan Soldier bulunamad�!");
            }
        }
       

    }

    private void OnTriggerEnter(Collider other)
    {
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null)
        {

            // Hedef bile�enini almay� dene
            if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
            {
                if (soldier == null)
                {
                    return;
                }

                // Tak�m ID'lerini kar��la�t�r ve logla
                var myTeam = soldier.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;

                // 3. D��MAN KONTROL�
                if (myTeam != otherTeam)
                {
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);

                        // Bu noktada, en iyi hedefi se�me ve controller'a atama mant��� devreye girer.
                        // �imdilik sadece yeni giren hedefi se�elim:
                        AssignBestTarget();
                        
                    }
                }
            }
            else if (potentialTargetParent.TryGetComponent<PlayerSC>(out var baseIdentity))
            {
                // Kendi birim bilgimizin varl���n� kontrol et (Hata korumas�)
                if (soldier == null)
                {
                    return;
                }

                // Tak�m ID'lerini kar��la�t�r ve logla
                var myTeam = soldier.TeamId.Value;
                var otherTeam = baseIdentity.TeamId.Value;

                // 3. D��MAN KONTROL�
                if (myTeam != otherTeam)
                {
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);
                        AssignBestTarget();

                    }
                }
            }
        }
        else
        {

        }
    }

    // --- YEN� LOGIC: HEDEF �IKARMA ---
    private void OnTriggerExit(Collider other)
    {
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null && potentialTargetParent.TryGetComponent<Soldier>(out _))
        {
            // Listeden ��karma i�lemi (Tak�m kontrol�ne gerek yok, listede olan ��kar�l�r)
            if (detectedTargets.Contains(potentialTargetParent))
            {
                detectedTargets.Remove(potentialTargetParent);
                if (controllerAttack.GetCurrentTarget() == potentialTargetParent)
                {
                    AssignBestTarget();
                }
            }
        }
    }

    /// <summary>
    /// Alg�lanan hedefler listesinden (Transform) asker birimine en yak�n olan� bulur.
    /// </summary>
    /// <param name="targets">Potansiyel d��man Transform listesi.</param>
    /// <returns>En yak�n d��man�n Transform'u; liste bo�sa null.</returns>
    private Transform FindClosestTarget(List<Transform> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return null;
        }

        Vector3 myPosition = transform.position; 

        Transform closestTarget = null;
        float minDistanceSq = float.MaxValue; 

        // T�m alg�lanan hedefleri d�ng�ye al
        for (int i = 0; i < targets.Count; i++)
        {
            Transform currentTarget = targets[i];

            if (currentTarget == null)
            {

                continue;
            }

            float distanceSq = (currentTarget.position - myPosition).sqrMagnitude;

            // Daha yak�n bir hedef bulundu mu?
            if (distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                closestTarget = currentTarget;
            }
        }

        return closestTarget;
    }


    /// <summary>
    /// Alg�lanan hedefler listesinden en uygun olan� se�er ve Controller'lara atar.
    /// (�imdi: En yak�n olan� se�er)
    /// </summary>
    public void AssignBestTarget()
    {
        detectedTargets.RemoveAll(t => t == null);

        if (detectedTargets.Count > 0)
        {
            // **YEN� MANTIK:** En Yak�n Hedefi Bul
            Transform newTarget = FindClosestTarget(detectedTargets);

            if (newTarget != null)
            {

                controllerNavMesh.GiveMeNewTarget(newTarget);
                controllerAttack.StartAttacking(newTarget);
            }
            else // Temizlik sonras� listede eleman kalmad�ysa (T�m hedefler null ��kt�ysa)
            {
                // Hi� hedef kalmad�ysa
                controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini sa�la
                controllerAttack.StopAttacking();
            }

        }
        else
        {
            // Hi� hedef kalmad�ysa
            controllerNavMesh.GiveMeNewTarget(null);
            controllerAttack.StopAttacking();
        }
    }
}
