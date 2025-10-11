using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    private SoldiersControllerNavMesh controllerNavMesh;
    private SoldiersAttackController controllerAttack;
    private Soldier soldier; // Kendi takým bilgimizi tutan referans
    [SerializeField] Transform target; // Þu an kullanýlmýyor, ama gelecekte hedefi tutmak için kullanýlabilir.


    // Potansiyel düþmanlarý tutan liste
    private List<Transform> detectedTargets = new List<Transform>();

    public void WhenNetworkSpawn()
    {
        // Yalnýzca Sunucuda gerekli bileþenleri çekme
        if (!IsServer) return;
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
        // Yalnýzca Sunucuda çalýþacak KRÝTÝK OYUN MANTIÐI
        if (!IsServer || soldier == null) return;

        // 1. GENEL LOG: Kimin neye çarptýðýný her iki tarafta da yazdýr.
        // Bu, debug için önemlidir, tetikleyicinin çalýþýp çalýþmadýðýný gösterir.
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null)
        {
            // Tetiklenme Logu: Parent nesnenin adýný yazdýrýr.
            Debug.Log($"[DETECTOR - FÝZÝK] Tetiklenme: {potentialTargetParent.name} (Parent) | Kendi Takým ID: {soldier.TeamId.Value}");

            // Hedef bileþenini almayý dene
            if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
            {
                // Kendi birim bilgimizin varlýðýný kontrol et (Hata korumasý)
                if (soldier == null)
                {
                    Debug.LogError("[SERVER DETECTOR] Kendi kimlik bilgisi (myIdentity) bulunamadý!");
                    return;
                }

                // Takým ID'lerini karþýlaþtýr ve logla
                var myTeam = soldier.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;
                Debug.Log($"[SERVER DETECTOR KRÝTÝK ANALÝZ] Kendi: {myTeam}, Diðer: {otherTeam} | Ýsim: {potentialTargetParent.name}");

                // 3. DÜÞMAN KONTROLÜ
                if (myTeam != otherTeam)
                {
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);
                        Debug.Log($"[SERVER DETECTOR] {potentialTargetParent.name} hedefler listesine eklendi. Toplam hedef: {detectedTargets.Count}");

                        // Liste boþken bir düþman geldiyse, saldýrý/yönlendirme kararý al.
                        // Bu mantýk, askerin her yeni düþman girdiðinde deðil,
                        // sadece þu anda bir hedefi yoksa yeni hedef seçmesini saðlar.
                        if (detectedTargets.Count == 1)
                        {
                            // Bu noktada, en iyi hedefi seçme ve controller'a atama mantýðý devreye girer.
                            // Þimdilik sadece yeni giren hedefi seçelim:
                            AssignBestTarget();
                        }
                    }
                }
            }
        }
        else
        {
            // Parent'ý olmayan objelerin (örneðin yerdeki bir powerup) tetiklenme logu
            Debug.Log($"[DETECTOR - FÝZÝK] Tetiklenme: {other.transform.name} (Kendi) | Kendi Takým ID: {soldier.TeamId.Value}");
        }
        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendiðini bildirir.
    }

    // --- YENÝ LOGIC: HEDEF ÇIKARMA ---
    private void OnTriggerExit(Collider other)
    {
        // Yalnýzca Sunucuda çalýþacak KRÝTÝK OYUN MANTIÐI
        if (!IsServer || soldier == null) return;

        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null && potentialTargetParent.TryGetComponent<Soldier>(out _))
        {
            // Listeden çýkarma iþlemi (Takým kontrolüne gerek yok, listede olan çýkarýlýr)
            if (detectedTargets.Contains(potentialTargetParent))
            {
                detectedTargets.Remove(potentialTargetParent);
                Debug.Log($"[SERVER DETECTOR] {potentialTargetParent.name} alandan çýktý ve listeden çýkarýldý. Kalan hedef: {detectedTargets.Count}");

                // Eðer çýkan birim þu anki hedefimizse, yeni bir hedef seçmeliyiz.
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

        // Kendi birimimizin pozisyonu (Genellikle bu komut dosyasýnýn parent'ýdýr)
        Vector3 myPosition = transform.position; // TargetDetector'ün veya parent'ýnýn pozisyonu

        Transform closestTarget = null;
        float minDistanceSq = float.MaxValue; // En büyük float deðeri ile baþla

        // Tüm algýlanan hedefleri döngüye al
        for (int i = 0; i < targets.Count; i++)
        {
            Transform currentTarget = targets[i];

            // Çeþitli nedenlerle (örneðin hedef henüz yok edilmiþ olabilir ama listeden çýkarýlmamýþ olabilir)
            // null kontrolü her zaman iyidir.
            if (currentTarget == null)
            {
                // Listeden null hedefleri temizleme, bu noktada kritik bir iþlem olabilir.
                // Basitlik için þimdilik atlýyoruz, ama ileride 'CleanDeadTargets' gibi bir metot eklenebilir.
                continue;
            }

            // Mesafe hesaplamasý
            // Vektör mesafesi yerine Vector3.sqrMagnitude (kare mesafesi) kullanmak, 
            // performansý artýrýr çünkü pahalý olan karekök (sqrt) hesaplamasýný atlarýz.
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
        // 1. Ölü hedefleri temizle
        // Bu, bir hedef yok edildiðinde ama OnTriggerExit henüz çalýþmadýðýnda oluþabilecek hatalarý engeller.
        detectedTargets.RemoveAll(t => t == null);

        if (detectedTargets.Count > 0)
        {
            // **YENÝ MANTIK:** En Yakýn Hedefi Bul
            Transform newTarget = FindClosestTarget(detectedTargets);

            if (newTarget != null)
            {
                // NavMesh ve Saldýrý Controller'larýna hedefi bildir.
                controllerNavMesh.GiveMeNewTarget(newTarget);
                controllerAttack.StartAttacking(newTarget);
                Debug.Log($"[SERVER DETECTOR] YENÝ HEDEF SEÇÝLDÝ (En Yakýn): {newTarget.name}");
            }
            else // Temizlik sonrasý listede eleman kalmadýysa (Tüm hedefler null çýktýysa)
            {
                // Hiç hedef kalmadýysa
                controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini saðla
                controllerAttack.StopAttacking();
                Debug.Log("[SERVER DETECTOR] Tüm hedefler null çýktý veya alandan çýktý, saldýrý durduruldu.");
            }

        }
        else
        {
            // Hiç hedef kalmadýysa
            controllerNavMesh.GiveMeNewTarget(null);
            controllerAttack.StopAttacking();
            Debug.Log("[SERVER DETECTOR] Tüm hedefler alandan çýktý, saldýrý durduruldu.");
        }
    }
}
