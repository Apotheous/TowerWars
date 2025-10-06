using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private Soldier myIdentity; // Kendi tak�m bilgimizi tutan referans
    [SerializeField] Transform target; // �u an kullan�lm�yor, ama gelecekte hedefi tutmak i�in kullan�labilir.

    public void WhenNetworkSpawn()
    {
        Debug.Log($"[SERVER DETECTOR] Ba�lad� OnNetworkSpawn");
        // SoldiersControllerNavMesh'e ula�man�n en sa�lam yolu:
        if (soldiersControllerNavMesh == null)
        {
            soldiersControllerNavMesh = GetComponentInParent<SoldiersControllerNavMesh>();
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh �ekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh zaten atanm��");
        }

        // Kendi UnitIdentity'mizi bul (tak�m bilgisi i�in kritik).
        myIdentity = GetComponentInParent<Soldier>();
        Debug.Log($"[SERVER DETECTOR] UnitIdentity �ekildi MyTeamID ===" + myIdentity.TeamId.Value +"++++");
        if (myIdentity == null)
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
            Debug.Log($"[DETECTOR - F�Z�K] Tetiklenme: {potentialTargetParent.name} (Parent) | Kendi Tak�m ID: {myIdentity.TeamId.Value}");

            // 2. YALNIZCA SUNUCUDA �ALI�ACAK KR�T�K OYUN MANTI�I
            // Bu birim Sunucu taraf�ndan kontrol ediliyorsa, hedefleme kararlar�n� al.
            if (IsServer) // IsServer kontrol�n� ekleyelim
            {
                // Hedef bile�enini almay� dene
                if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
                {
                    // Kendi birim bilgimizin varl���n� kontrol et (Hata korumas�)
                    if (myIdentity == null)
                    {
                        Debug.LogError("[SERVER DETECTOR] Kendi kimlik bilgisi (myIdentity) bulunamad�!");
                        return;
                    }

                    // Tak�m ID'lerini kar��la�t�r ve logla
                    var myTeam = myIdentity.TeamId.Value;
                    var otherTeam = unitIdentity.TeamId.Value;
                    Debug.Log($"[SERVER DETECTOR KR�T�K ANAL�Z] Kendi: {myTeam}, Di�er: {otherTeam} | �sim: {potentialTargetParent.name}");

                    // 3. D��MAN KONTROL�
                    if (myTeam != otherTeam)
                    {
                        // D��man Tespit Edildi Logu
                        Debug.Log($"[SERVER DETECTOR] D��MAN TESP�T ED�LD�! Hedef: {potentialTargetParent.name}");

                        // Hedefle ilgili kararlar� (durma, hedef atama) SADECE SUNUCU al�r.
                        soldiersControllerNavMesh.StopUnit();
                        // soldiersControllerNavMesh.GiveMeNewTarget(potentialTargetParent); 

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
            Debug.Log($"[DETECTOR - F�Z�K] Tetiklenme: {other.transform.name} (Kendi) | Kendi Tak�m ID: {myIdentity.TeamId.Value}");
        }
        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendi�ini bildirir.

        if (other.transform.parent != null)
        {
            target = other.transform.parent;
            Debug.Log($"[SERVER DETECTOR] trigger tetiklendi Panet�n�n ({target.name}) -------MyTeamID" + myIdentity.TeamId.Value);
            // 2. Birim Kimli�i Kontrol�: �arpt���m�z objenin bir UnitIdentity'si var m�?
            if (target.TryGetComponent<Soldier>(out var unitIdentity))
            {

                Debug.LogError("TargetDetector i�in gerekli olan myIdentity (kendi birim bilgisi) �ekildi");
                // Kendi kimlik bilgimiz ayarl� de�ilse devam etme (Hata korumas�)
                if (myIdentity == null)
                {
                    Debug.LogError("TargetDetector i�in gerekli olan myIdentity (kendi birim bilgisi) null!");
                    return;
                }
                // YEN� KR�T�K KONTROL - TeamID'leri netle�tir
                var myTeam = myIdentity.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;
                // 3. KR�T�K HATA AYIKLAMA LOGU: Tak�m ID'lerini kar��la�t�r�r.
                // Bu log, blo�a girilip girilmedi�ini anlaman�n anahtar�d�r.

                Debug.Log($"[SERVER DETECTOR KR�T�K ANAL�Z] Kendi: {myTeam}, Di�er: {otherTeam} + �sim: {target.name}");
                // 4. D��man Kontrol�: Tak�m ID'miz ile �arpt���m�z birimin Tak�m ID'si farkl�ysa (Yani d��mansa)
                if (myIdentity.TeamId.Value != unitIdentity.TeamId.Value)
                {
                    // D��man Tespit Edildi Loglar�
                    Debug.Log($"[SERVER DETECTOR] D��MAN TESP�T ED�LD�! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");

                    // Yak�nl�k, mesafe, �ncelik vb. KONTROL� OLMADAN hemen hedefi de�i�tir.
                    soldiersControllerNavMesh.StopUnit();
                    //soldiersControllerNavMesh.GiveMeNewTarget(target);

                    // Hedef Atama Logu
                    Debug.Log($"[SERVER DETECTOR] D��man birim ({target}) menzile girdi. Yeni hedef atand�.");
                }

            }
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR] trigger tetiklendi kendi ({other.transform.name}) -------MyTeamID" + myIdentity.TeamId.Value);
        }
    }
}
