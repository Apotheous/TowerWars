using Unity.Netcode;
using UnityEngine;

public class TargetDetector : MonoBehaviour
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
        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendi�ini bildirir.
        
        if (other.transform.parent!=null)
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
