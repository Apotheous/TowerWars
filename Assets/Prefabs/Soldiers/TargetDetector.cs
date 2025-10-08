using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    [SerializeField] private SoldiersControllerNavMesh controllerNavMesh;
    [SerializeField] private SoldiersAttackController controllerAttack;
    [SerializeField] private Soldier soldier; // Kendi tak�m bilgimizi tutan referans
    [SerializeField] Transform target; // �u an kullan�lm�yor, ama gelecekte hedefi tutmak i�in kullan�labilir.

    public void WhenNetworkSpawn()
    {
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
        soldier = GetComponentInParent<Soldier>();
        Debug.Log($"[SERVER DETECTOR] UnitIdentity �ekildi MyTeamID ===" + soldier.TeamId.Value +"++++");
        if (soldier == null)
        {
            Debug.LogError("TargetDetector i�in gerekli olan UnitIdentity bulunamad�!");

        }

    }

    private void OnTriggerEnter(Collider other)
    {

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
                        // D��man Tespit Edildi Logu
                        Debug.Log($"[SERVER DETECTOR] D��MAN TESP�T ED�LD�! Hedef: {potentialTargetParent.name}");

                        // Hedefle ilgili kararlar� (durma, hedef atama) SADECE SUNUCU al�r.
                        //controllerNavMesh.StopUnit();
                        controllerNavMesh.GiveMeNewTarget(potentialTargetParent);
                        controllerAttack.StartAttacking(potentialTargetParent);

                        // A� �zerindeki t�m istemcilere birimin durdu�u bilgisini Network/RPC ile g�ndermeniz gerekebilir.
                        // Bu, NavMesh Agent'�n durma i�leminin yerel olarak g�rselle�tirilmesini sa�lar.

                        Debug.Log($"[SERVER DETECTOR] Durma emri verildi ve yeni hedef karar� al�n�yor.");
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
}
