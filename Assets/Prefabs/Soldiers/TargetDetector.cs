using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    private SoldiersControllerNavMesh controllerNavMesh;
    private SoldiersAttackController controllerAttack;
    private Soldier soldier; // Kendi tak�m bilgimizi tutan referans
    [SerializeField] Transform target; // �u an kullan�lm�yor, ama gelecekte hedefi tutmak i�in kullan�labilir.


    // Potansiyel d��manlar� tutan liste
    private List<Transform> detectedTargets = new List<Transform>();

    public void WhenNetworkSpawn()
    {
        // Yaln�zca Sunucuda gerekli bile�enleri �ekme
        if (!IsServer) return;
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
        // Yaln�zca Sunucuda �al��acak KR�T�K OYUN MANTI�I
        if (!IsServer || soldier == null) return;

        // 1. GENEL LOG: Kimin neye �arpt���n� her iki tarafta da yazd�r.
        // Bu, debug i�in �nemlidir, tetikleyicinin �al���p �al��mad���n� g�sterir.
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null)
        {
            // Tetiklenme Logu: Parent nesnenin ad�n� yazd�r�r.
            Debug.Log($"[DETECTOR - F�Z�K] Tetiklenme: {potentialTargetParent.name} (Parent) | Kendi Tak�m ID: {soldier.TeamId.Value}");

            // Hedef bile�enini almay� dene
            if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
            {
                // Kendi birim bilgimizin varl���n� kontrol et (Hata korumas�)
                if (soldier == null)
                {
                    Debug.LogError("[SERVER DETECTOR] Kendi kimlik bilgisi (myIdentity) bulunamad�!");
                    return;
                }

                // Tak�m ID'lerini kar��la�t�r ve logla
                var myTeam = soldier.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;
                Debug.Log($"[SERVER DETECTOR KR�T�K ANAL�Z] Kendi: {myTeam}, Di�er: {otherTeam} | �sim: {potentialTargetParent.name}");

                // 3. D��MAN KONTROL�
                if (myTeam != otherTeam)
                {
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);
                        Debug.Log($"[SERVER DETECTOR] {potentialTargetParent.name} hedefler listesine eklendi. Toplam hedef: {detectedTargets.Count}");

                        // Liste bo�ken bir d��man geldiyse, sald�r�/y�nlendirme karar� al.
                        // Bu mant�k, askerin her yeni d��man girdi�inde de�il,
                        // sadece �u anda bir hedefi yoksa yeni hedef se�mesini sa�lar.
                        if (detectedTargets.Count == 1)
                        {
                            // Bu noktada, en iyi hedefi se�me ve controller'a atama mant��� devreye girer.
                            // �imdilik sadece yeni giren hedefi se�elim:
                            AssignBestTarget();
                        }
                    }
                }
            }
        }
        else
        {
            // Parent'� olmayan objelerin (�rne�in yerdeki bir powerup) tetiklenme logu
            Debug.Log($"[DETECTOR - F�Z�K] Tetiklenme: {other.transform.name} (Kendi) | Kendi Tak�m ID: {soldier.TeamId.Value}");
        }
        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendi�ini bildirir.
    }

    // --- YEN� LOGIC: HEDEF �IKARMA ---
    private void OnTriggerExit(Collider other)
    {
        // Yaln�zca Sunucuda �al��acak KR�T�K OYUN MANTI�I
        if (!IsServer || soldier == null) return;

        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null && potentialTargetParent.TryGetComponent<Soldier>(out _))
        {
            // Listeden ��karma i�lemi (Tak�m kontrol�ne gerek yok, listede olan ��kar�l�r)
            if (detectedTargets.Contains(potentialTargetParent))
            {
                detectedTargets.Remove(potentialTargetParent);
                Debug.Log($"[SERVER DETECTOR] {potentialTargetParent.name} alandan ��kt� ve listeden ��kar�ld�. Kalan hedef: {detectedTargets.Count}");

                // E�er ��kan birim �u anki hedefimizse, yeni bir hedef se�meliyiz.
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

        // Kendi birimimizin pozisyonu (Genellikle bu komut dosyas�n�n parent'�d�r)
        Vector3 myPosition = transform.position; // TargetDetector'�n veya parent'�n�n pozisyonu

        Transform closestTarget = null;
        float minDistanceSq = float.MaxValue; // En b�y�k float de�eri ile ba�la

        // T�m alg�lanan hedefleri d�ng�ye al
        for (int i = 0; i < targets.Count; i++)
        {
            Transform currentTarget = targets[i];

            // �e�itli nedenlerle (�rne�in hedef hen�z yok edilmi� olabilir ama listeden ��kar�lmam�� olabilir)
            // null kontrol� her zaman iyidir.
            if (currentTarget == null)
            {
                // Listeden null hedefleri temizleme, bu noktada kritik bir i�lem olabilir.
                // Basitlik i�in �imdilik atl�yoruz, ama ileride 'CleanDeadTargets' gibi bir metot eklenebilir.
                continue;
            }

            // Mesafe hesaplamas�
            // Vekt�r mesafesi yerine Vector3.sqrMagnitude (kare mesafesi) kullanmak, 
            // performans� art�r�r ��nk� pahal� olan karek�k (sqrt) hesaplamas�n� atlar�z.
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
        // 1. �l� hedefleri temizle
        // Bu, bir hedef yok edildi�inde ama OnTriggerExit hen�z �al��mad���nda olu�abilecek hatalar� engeller.
        detectedTargets.RemoveAll(t => t == null);

        if (detectedTargets.Count > 0)
        {
            // **YEN� MANTIK:** En Yak�n Hedefi Bul
            Transform newTarget = FindClosestTarget(detectedTargets);

            if (newTarget != null)
            {
                // NavMesh ve Sald�r� Controller'lar�na hedefi bildir.
                controllerNavMesh.GiveMeNewTarget(newTarget);
                controllerAttack.StartAttacking(newTarget);
                Debug.Log($"[SERVER DETECTOR] YEN� HEDEF SE��LD� (En Yak�n): {newTarget.name}");
            }
            else // Temizlik sonras� listede eleman kalmad�ysa (T�m hedefler null ��kt�ysa)
            {
                // Hi� hedef kalmad�ysa
                controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini sa�la
                controllerAttack.StopAttacking();
                Debug.Log("[SERVER DETECTOR] T�m hedefler null ��kt� veya alandan ��kt�, sald�r� durduruldu.");
            }

        }
        else
        {
            // Hi� hedef kalmad�ysa
            controllerNavMesh.GiveMeNewTarget(null);
            controllerAttack.StopAttacking();
            Debug.Log("[SERVER DETECTOR] T�m hedefler alandan ��kt�, sald�r� durduruldu.");
        }
    }
}
