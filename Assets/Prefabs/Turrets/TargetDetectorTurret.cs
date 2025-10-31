using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDetectorTurret : MonoBehaviour, IIgnoreCollision
{
    private TurretsRotationController controllerNavMesh;
    private TurretsAttackController controllerAttack;
    private Turret TurretMyId; // Kendi takým bilgimizi tutan referans
    private Soldier soldierId; // Kendi takým bilgimizi tutan referans


    // Potansiyel düþmanlarý tutan liste
    private List<Transform> detectedTargets = new List<Transform>();


    public void WhenNetworkSpawn()
    {
        Debug.Log($"[SERVER DETECTOR Turret] WhenNetworkSpawn çaðrýldý."); // Baþlangýç Debug

        //// Yalnýzca Sunucuda gerekli bileþenleri çekme
        //if (!IsServer) return; // Sunucu kontrolü aktif deðilse tümü çalýþýr (Unity Editor Testi)

        // SoldiersControllerNavMesh'e ulaþmanýn en saðlam yolu:
        if (controllerNavMesh == null)
        {
            controllerNavMesh = GetComponentInParent<TurretsRotationController>();
            Debug.Log($"[SERVER DETECTOR Turret] TurretsRotationController çekildi.");
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] TurretsRotationController zaten atanmýþ");
        }
        if (controllerAttack == null)
        {
            controllerAttack = GetComponentInParent<TurretsAttackController>();
            Debug.Log($"[SERVER DETECTOR Turret] TurretsAttackController çekildi.");
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] TurretsAttackController zaten atanmýþ");
        }
        // Kendi UnitIdentity'mizi bul (takým bilgisi için kritik).
        if (TurretMyId == null)
        {
            TurretMyId = GetComponentInParent<Turret>();
            if (TurretMyId != null)
            {
                Debug.Log($"[SERVER DETECTOR Turret] Turret çekildi. MyTeamID ==={TurretMyId.TeamId.Value}++++");
            }
            else
            {
                Debug.LogError("[SERVER DETECTOR Turret] TargetDetector için gerekli olan Turret bulunamadý!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SERVER DETECTOR Turret] OnTriggerEnter tetiklendi. Collider: {other.name}");
        if (other.gameObject.name == "TargetDetector" || other.gameObject.name == "bullet(Clone)")
        {
            Debug.Log($"[SERVER DETECTOR Turret] Tetiklenen Collider bir TargetDetector veya bullet. Yoksayýlýyor.");
            return;
        }
        Transform potentialTargetParent = other.transform;

        if (potentialTargetParent != null)
        {
            Debug.Log($"[SERVER DETECTOR Turret] Potansiyel hedef Parent: {potentialTargetParent.name}");

            // Hedef bileþenini almayý dene
            if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
            {
                Debug.Log($"[SERVER DETECTOR Turret] Hedefte Soldier (Unit) bileþeni bulundu.");
                if (TurretMyId == null)
                {
                    Debug.LogWarning("[SERVER DETECTOR Turret] TurretMyId (Takým Bilgisi) bulunmadýðý için çýkýlýyor.");
                    return;
                }

                // Takým ID'lerini karþýlaþtýr ve logla
                var myTeam = TurretMyId.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;
                Debug.Log($"[SERVER DETECTOR Turret] Takým Kontrolü: Benim Takýmým={myTeam}, Diðer Takým={otherTeam}");

                // 3. DÜÞMAN KONTROLÜ
                if (myTeam != otherTeam)
                {
                    Debug.Log($"[SERVER DETECTOR Turret] Takýmlar farklý. Düþman Olarak Deðerlendirildi.");
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);
                        Debug.Log($"[SERVER DETECTOR Turret] Yeni Düþman listeye eklendi: {potentialTargetParent.name}. Toplam: {detectedTargets.Count}");

                        AssignBestTarget();
                    }
                    else
                    {
                        Debug.Log($"[SERVER DETECTOR Turret] Düþman zaten listede.");
                    }
                }
                else
                {
                    Debug.Log($"[SERVER DETECTOR Turret] Ayný takým. Yoksayýlýyor.");
                }
            }
            else if (potentialTargetParent.TryGetComponent<PlayerSC>(out var baseIdentity))
            {
                Debug.Log($"[SERVER DETECTOR Turret] Muhtemelen KendiBase ile Triggerlandý");
                return;
            }
            else
            {
                Debug.Log($"[SERVER DETECTOR Turret] Hedefte Soldier veya PlayerSC bileþeni bulunamadý.==" + other.name );
            }
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] Tetiklenen Collider'ýn Parent'ý (potentialTargetParent) null. == " + other.name);
        }
    }

    // --- YENÝ LOGIC: HEDEF ÇIKARMA ---
    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[SERVER DETECTOR Turret] OnTriggerExit tetiklendi. Collider: {other.name}");
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null && potentialTargetParent.TryGetComponent<Soldier>(out _))
        {
            // Listeden çýkarma iþlemi (Takým kontrolüne gerek yok, listede olan çýkarýlýr)
            if (detectedTargets.Contains(potentialTargetParent))
            {
                detectedTargets.Remove(potentialTargetParent);
                Debug.Log($"[SERVER DETECTOR Turret] Hedef listeden çýkarýldý: {potentialTargetParent.name}. Kalan: {detectedTargets.Count}");

                // Eðer çýkan hedef, o anda saldýrýlan hedef ise, yeni bir hedef seç
                if (controllerAttack.GetCurrentTarget() == potentialTargetParent)
                {
                    Debug.Log($"[SERVER DETECTOR Turret] Çýkan hedef mevcut saldýrý hedefiydi. Yeni hedef aranýyor.");
                    AssignBestTarget();
                }
                else
                {
                    Debug.Log($"[SERVER DETECTOR Turret] Çýkan hedef, mevcut saldýrý hedefi deðildi.");
                }
            }
            else
            {
                Debug.Log($"[SERVER DETECTOR Turret] Çýkan birim listede deðildi.");
            }
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] Çýkan birim geçerli bir Soldier deðildi.");
        }
    }

    /// <summary>
    /// Algýlanan hedefler listesinden (Transform) asker birimine en yakýn olaný bulur.
    /// </summary>
    /// <param name="targets">Potansiyel düþman Transform listesi.</param>
    /// <returns>En yakýn düþmanýn Transform'u; liste boþsa null.</returns>
    private Transform FindClosestTarget(List<Transform> targets)
    {
        Debug.Log($"[SERVER DETECTOR Turret] FindClosestTarget çaðrýldý. Listede {targets?.Count ?? 0} hedef var.");
        if (targets == null || targets.Count == 0)
        {
            Debug.Log($"[SERVER DETECTOR Turret] Hedef listesi boþ. Null dönülüyor.");
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
                Debug.LogWarning($"[SERVER DETECTOR Turret] Listedeki bir hedef (indeks {i}) null çýktý. Atlanýyor.");
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

        Debug.Log($"[SERVER DETECTOR Turret] En yakýn hedef bulundu: {(closestTarget != null ? closestTarget.name : "Yok")}");
        return closestTarget;
    }


    /// <summary>
    /// Algýlanan hedefler listesinden en uygun olaný seçer ve Controller'lara atar.
    /// (Þimdi: En yakýn olaný seçer)
    /// </summary>
    public void AssignBestTarget()
    {
        Debug.Log($"[SERVER DETECTOR Turret] AssignBestTarget çaðrýldý. Mevcut Hedef Sayýsý: {detectedTargets.Count}");

        detectedTargets.RemoveAll(t => t == null);
        Debug.Log($"[SERVER DETECTOR Turret] Null hedefler temizlendi. Kalan Hedef Sayýsý: {detectedTargets.Count}");


        if (detectedTargets.Count > 0)
        {
            // **YENÝ MANTIK:** En Yakýn Hedefi Bul
            Transform newTarget = FindClosestTarget(detectedTargets);

            if (newTarget != null)
            {
                Debug.Log($"[SERVER DETECTOR Turret] Controller'lara yeni hedef atanýyor: {newTarget.name}");

                controllerNavMesh.GiveMeNewTarget(newTarget);
                controllerAttack.StartAttacking(newTarget);
            }
            else // Temizlik sonrasý listede eleman kalmadýysa (Tüm hedefler null çýktýysa)
            {
                Debug.Log("[SERVER DETECTOR Turret] Temizlik sonrasý listede geçerli hedef kalmadý (Tümü null çýktý).");
                // Hiç hedef kalmadýysa
                controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini saðla
                controllerAttack.StopAttacking();
            }

        }
        else
        {
            Debug.Log("[SERVER DETECTOR Turret] Hedef listesi boþ. Controller'lardan hedef temizleniyor.");
            // Hiç hedef kalmadýysa
            controllerNavMesh.GiveMeNewTarget(null);
            controllerAttack.StopAttacking();
        }
    }
}
