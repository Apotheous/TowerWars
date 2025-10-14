using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    private SoldiersControllerNavMesh controllerNavMesh;
    private SoldiersAttackController controllerAttack;
    private Soldier soldier; // Kendi takým bilgimizi tutan referans


    // Potansiyel düþmanlarý tutan liste
    private List<Transform> detectedTargets = new List<Transform>();

    public void WhenNetworkSpawn()
    {
        //// Yalnýzca Sunucuda gerekli bileþenleri çekme
        //if (!IsServer) return;
        Debug.Log($"[SERVER DETECTOR] Baþladý OnNetworkSpawn");
        // SoldiersControllerNavMesh'e ulaþmanýn en saðlam yolu:
        if (controllerNavMesh == null)
        {
            controllerNavMesh = GetComponentInParent<SoldiersControllerNavMesh>();
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh çekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh zaten atanmýþ");
        }
        if (controllerAttack == null)
        {
            controllerAttack = GetComponentInParent<SoldiersAttackController>();
            Debug.Log($"[SERVER DETECTOR] SoldiersAttackController çekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersAttackController zaten atanmýþ");
        }
        // Kendi UnitIdentity'mizi bul (takým bilgisi için kritik).
        if (soldier == null)
        {
            soldier = GetComponentInParent<Soldier>();
            if (soldier != null)
            {
                Debug.Log($"[SERVER DETECTOR] Soldier çekildi MyTeamID ===" + soldier.TeamId.Value + "++++");
            }
            else
            {
                Debug.LogError("TargetDetector için gerekli olan Soldier bulunamadý!");
            }
        }
       

    }

    private void OnTriggerEnter(Collider other)
    {
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null)
        {

            // Hedef bileþenini almayý dene
            if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
            {
                if (soldier == null)
                {
                    return;
                }

                // Takým ID'lerini karþýlaþtýr ve logla
                var myTeam = soldier.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;

                // 3. DÜÞMAN KONTROLÜ
                if (myTeam != otherTeam)
                {
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);

                        // Bu noktada, en iyi hedefi seçme ve controller'a atama mantýðý devreye girer.
                        // Þimdilik sadece yeni giren hedefi seçelim:
                        AssignBestTarget();
                        
                    }
                }
            }
            else if (potentialTargetParent.TryGetComponent<PlayerSC>(out var baseIdentity))
            {
                // Kendi birim bilgimizin varlýðýný kontrol et (Hata korumasý)
                if (soldier == null)
                {
                    return;
                }

                // Takým ID'lerini karþýlaþtýr ve logla
                var myTeam = soldier.TeamId.Value;
                var otherTeam = baseIdentity.TeamId.Value;

                // 3. DÜÞMAN KONTROLÜ
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

    // --- YENÝ LOGIC: HEDEF ÇIKARMA ---
    private void OnTriggerExit(Collider other)
    {
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null && potentialTargetParent.TryGetComponent<Soldier>(out _))
        {
            // Listeden çýkarma iþlemi (Takým kontrolüne gerek yok, listede olan çýkarýlýr)
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
    /// Algýlanan hedefler listesinden (Transform) asker birimine en yakýn olaný bulur.
    /// </summary>
    /// <param name="targets">Potansiyel düþman Transform listesi.</param>
    /// <returns>En yakýn düþmanýn Transform'u; liste boþsa null.</returns>
    private Transform FindClosestTarget(List<Transform> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return null;
        }

        Vector3 myPosition = transform.position; 

        Transform closestTarget = null;
        float minDistanceSq = float.MaxValue; 

        // Tüm algýlanan hedefleri döngüye al
        for (int i = 0; i < targets.Count; i++)
        {
            Transform currentTarget = targets[i];

            if (currentTarget == null)
            {

                continue;
            }

            float distanceSq = (currentTarget.position - myPosition).sqrMagnitude;

            // Daha yakýn bir hedef bulundu mu?
            if (distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                closestTarget = currentTarget;
            }
        }

        return closestTarget;
    }


    /// <summary>
    /// Algýlanan hedefler listesinden en uygun olaný seçer ve Controller'lara atar.
    /// (Þimdi: En yakýn olaný seçer)
    /// </summary>
    public void AssignBestTarget()
    {
        detectedTargets.RemoveAll(t => t == null);

        if (detectedTargets.Count > 0)
        {
            // **YENÝ MANTIK:** En Yakýn Hedefi Bul
            Transform newTarget = FindClosestTarget(detectedTargets);

            if (newTarget != null)
            {

                controllerNavMesh.GiveMeNewTarget(newTarget);
                controllerAttack.StartAttacking(newTarget);
            }
            else // Temizlik sonrasý listede eleman kalmadýysa (Tüm hedefler null çýktýysa)
            {
                // Hiç hedef kalmadýysa
                controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini saðla
                controllerAttack.StopAttacking();
            }

        }
        else
        {
            // Hiç hedef kalmadýysa
            controllerNavMesh.GiveMeNewTarget(null);
            controllerAttack.StopAttacking();
        }
    }
}
