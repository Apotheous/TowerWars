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

            // 2. YALNIZCA SUNUCUDA �ALI�ACAK KR�T�K OYUN MANTI�I
            // Bu birim Sunucu taraf�ndan kontrol ediliyorsa, hedefleme kararlar�n� al.
            if (IsServer) // IsServer kontrol�n� ekleyelim
            {
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
    /// Alg�lanan hedefler listesinden en uygun olan� se�er ve Controller'lara atar.
    /// (�imdilik: Sadece listedeki ilk eleman� se�er)
    /// </summary>
    private void AssignBestTarget()
    {
        if (detectedTargets.Count > 0)
        {
            // **GER�EK OYUN MANTI�I BURAYA GELMEL�:**
            // �rne�in: enYak�nHedef = FindClosestTarget(detectedTargets);
            // �imdilik: Liste doluysa ilk eleman� se�
            Transform newTarget = detectedTargets[0];

            // NavMesh ve Sald�r� Controller'lar�na hedefi bildir.
            controllerNavMesh.GiveMeNewTarget(newTarget);
            controllerAttack.StartAttacking(newTarget);
            Debug.Log($"[SERVER DETECTOR] YEN� HEDEF SE��LD�: {newTarget.name}");
        }
        else
        {
            // Hi� hedef kalmad�ysa
            controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini sa�la
            controllerAttack.StopAttacking();
            Debug.Log("[SERVER DETECTOR] T�m hedefler alandan ��kt�, sald�r� durduruldu.");
        }
    }
}
