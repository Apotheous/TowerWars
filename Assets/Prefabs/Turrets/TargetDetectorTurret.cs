using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDetectorTurret : MonoBehaviour, IIgnoreCollision
{
    private TurretsRotationController controllerNavMesh;
    private TurretsAttackController controllerAttack;
    private Turret TurretMyId; // Kendi tak�m bilgimizi tutan referans
    private Soldier soldierId; // Kendi tak�m bilgimizi tutan referans


    // Potansiyel d��manlar� tutan liste
    private List<Transform> detectedTargets = new List<Transform>();


    public void WhenNetworkSpawn()
    {
        Debug.Log($"[SERVER DETECTOR Turret] WhenNetworkSpawn �a�r�ld�."); // Ba�lang�� Debug

        //// Yaln�zca Sunucuda gerekli bile�enleri �ekme
        //if (!IsServer) return; // Sunucu kontrol� aktif de�ilse t�m� �al���r (Unity Editor Testi)

        // SoldiersControllerNavMesh'e ula�man�n en sa�lam yolu:
        if (controllerNavMesh == null)
        {
            controllerNavMesh = GetComponentInParent<TurretsRotationController>();
            Debug.Log($"[SERVER DETECTOR Turret] TurretsRotationController �ekildi.");
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] TurretsRotationController zaten atanm��");
        }
        if (controllerAttack == null)
        {
            controllerAttack = GetComponentInParent<TurretsAttackController>();
            Debug.Log($"[SERVER DETECTOR Turret] TurretsAttackController �ekildi.");
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] TurretsAttackController zaten atanm��");
        }
        // Kendi UnitIdentity'mizi bul (tak�m bilgisi i�in kritik).
        if (TurretMyId == null)
        {
            TurretMyId = GetComponentInParent<Turret>();
            if (TurretMyId != null)
            {
                Debug.Log($"[SERVER DETECTOR Turret] Turret �ekildi. MyTeamID ==={TurretMyId.TeamId.Value}++++");
            }
            else
            {
                Debug.LogError("[SERVER DETECTOR Turret] TargetDetector i�in gerekli olan Turret bulunamad�!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SERVER DETECTOR Turret] OnTriggerEnter tetiklendi. Collider: {other.name}");
        if (other.gameObject.name == "TargetDetector" || other.gameObject.name == "bullet(Clone)")
        {
            Debug.Log($"[SERVER DETECTOR Turret] Tetiklenen Collider bir TargetDetector veya bullet. Yoksay�l�yor.");
            return;
        }
        Transform potentialTargetParent = other.transform;

        if (potentialTargetParent != null)
        {
            Debug.Log($"[SERVER DETECTOR Turret] Potansiyel hedef Parent: {potentialTargetParent.name}");

            // Hedef bile�enini almay� dene
            if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
            {
                Debug.Log($"[SERVER DETECTOR Turret] Hedefte Soldier (Unit) bile�eni bulundu.");
                if (TurretMyId == null)
                {
                    Debug.LogWarning("[SERVER DETECTOR Turret] TurretMyId (Tak�m Bilgisi) bulunmad��� i�in ��k�l�yor.");
                    return;
                }

                // Tak�m ID'lerini kar��la�t�r ve logla
                var myTeam = TurretMyId.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;
                Debug.Log($"[SERVER DETECTOR Turret] Tak�m Kontrol�: Benim Tak�m�m={myTeam}, Di�er Tak�m={otherTeam}");

                // 3. D��MAN KONTROL�
                if (myTeam != otherTeam)
                {
                    Debug.Log($"[SERVER DETECTOR Turret] Tak�mlar farkl�. D��man Olarak De�erlendirildi.");
                    // Listede zaten yoksa listeye ekle
                    if (!detectedTargets.Contains(potentialTargetParent))
                    {
                        detectedTargets.Add(potentialTargetParent);
                        Debug.Log($"[SERVER DETECTOR Turret] Yeni D��man listeye eklendi: {potentialTargetParent.name}. Toplam: {detectedTargets.Count}");

                        AssignBestTarget();
                    }
                    else
                    {
                        Debug.Log($"[SERVER DETECTOR Turret] D��man zaten listede.");
                    }
                }
                else
                {
                    Debug.Log($"[SERVER DETECTOR Turret] Ayn� tak�m. Yoksay�l�yor.");
                }
            }
            else if (potentialTargetParent.TryGetComponent<PlayerSC>(out var baseIdentity))
            {
                Debug.Log($"[SERVER DETECTOR Turret] Muhtemelen KendiBase ile Triggerland�");
                return;
            }
            else
            {
                Debug.Log($"[SERVER DETECTOR Turret] Hedefte Soldier veya PlayerSC bile�eni bulunamad�.==" + other.name );
            }
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] Tetiklenen Collider'�n Parent'� (potentialTargetParent) null. == " + other.name);
        }
    }

    // --- YEN� LOGIC: HEDEF �IKARMA ---
    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[SERVER DETECTOR Turret] OnTriggerExit tetiklendi. Collider: {other.name}");
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null && potentialTargetParent.TryGetComponent<Soldier>(out _))
        {
            // Listeden ��karma i�lemi (Tak�m kontrol�ne gerek yok, listede olan ��kar�l�r)
            if (detectedTargets.Contains(potentialTargetParent))
            {
                detectedTargets.Remove(potentialTargetParent);
                Debug.Log($"[SERVER DETECTOR Turret] Hedef listeden ��kar�ld�: {potentialTargetParent.name}. Kalan: {detectedTargets.Count}");

                // E�er ��kan hedef, o anda sald�r�lan hedef ise, yeni bir hedef se�
                if (controllerAttack.GetCurrentTarget() == potentialTargetParent)
                {
                    Debug.Log($"[SERVER DETECTOR Turret] ��kan hedef mevcut sald�r� hedefiydi. Yeni hedef aran�yor.");
                    AssignBestTarget();
                }
                else
                {
                    Debug.Log($"[SERVER DETECTOR Turret] ��kan hedef, mevcut sald�r� hedefi de�ildi.");
                }
            }
            else
            {
                Debug.Log($"[SERVER DETECTOR Turret] ��kan birim listede de�ildi.");
            }
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR Turret] ��kan birim ge�erli bir Soldier de�ildi.");
        }
    }

    /// <summary>
    /// Alg�lanan hedefler listesinden (Transform) asker birimine en yak�n olan� bulur.
    /// </summary>
    /// <param name="targets">Potansiyel d��man Transform listesi.</param>
    /// <returns>En yak�n d��man�n Transform'u; liste bo�sa null.</returns>
    private Transform FindClosestTarget(List<Transform> targets)
    {
        Debug.Log($"[SERVER DETECTOR Turret] FindClosestTarget �a�r�ld�. Listede {targets?.Count ?? 0} hedef var.");
        if (targets == null || targets.Count == 0)
        {
            Debug.Log($"[SERVER DETECTOR Turret] Hedef listesi bo�. Null d�n�l�yor.");
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
                Debug.LogWarning($"[SERVER DETECTOR Turret] Listedeki bir hedef (indeks {i}) null ��kt�. Atlan�yor.");
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

        Debug.Log($"[SERVER DETECTOR Turret] En yak�n hedef bulundu: {(closestTarget != null ? closestTarget.name : "Yok")}");
        return closestTarget;
    }


    /// <summary>
    /// Alg�lanan hedefler listesinden en uygun olan� se�er ve Controller'lara atar.
    /// (�imdi: En yak�n olan� se�er)
    /// </summary>
    public void AssignBestTarget()
    {
        Debug.Log($"[SERVER DETECTOR Turret] AssignBestTarget �a�r�ld�. Mevcut Hedef Say�s�: {detectedTargets.Count}");

        detectedTargets.RemoveAll(t => t == null);
        Debug.Log($"[SERVER DETECTOR Turret] Null hedefler temizlendi. Kalan Hedef Say�s�: {detectedTargets.Count}");


        if (detectedTargets.Count > 0)
        {
            // **YEN� MANTIK:** En Yak�n Hedefi Bul
            Transform newTarget = FindClosestTarget(detectedTargets);

            if (newTarget != null)
            {
                Debug.Log($"[SERVER DETECTOR Turret] Controller'lara yeni hedef atan�yor: {newTarget.name}");

                controllerNavMesh.GiveMeNewTarget(newTarget);
                controllerAttack.StartAttacking(newTarget);
            }
            else // Temizlik sonras� listede eleman kalmad�ysa (T�m hedefler null ��kt�ysa)
            {
                Debug.Log("[SERVER DETECTOR Turret] Temizlik sonras� listede ge�erli hedef kalmad� (T�m� null ��kt�).");
                // Hi� hedef kalmad�ysa
                controllerNavMesh.GiveMeNewTarget(null); // veya bir sonraki default hedefine gitmesini sa�la
                controllerAttack.StopAttacking();
            }

        }
        else
        {
            Debug.Log("[SERVER DETECTOR Turret] Hedef listesi bo�. Controller'lardan hedef temizleniyor.");
            // Hi� hedef kalmad�ysa
            controllerNavMesh.GiveMeNewTarget(null);
            controllerAttack.StopAttacking();
        }
    }
}
