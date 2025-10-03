using Unity.Netcode;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    private SoldiersControllerNavMesh soldiersControllerNavMesh;
    private Soldier myIdentity; // Kendi tak�m bilgimizi tutan referans

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

        // 1. Sunucu Kontrol�: Hareket kararlar� sadece sunucuda al�n�r.
        if (soldiersControllerNavMesh.IsServer)
        {

            // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendi�ini bildirir.
            Debug.Log($"[SERVER DETECTOR] trigger tetiklendi ({other.name}) -------MyTeamID" + myIdentity.TeamId.Value);

            // 2. Birim Kimli�i Kontrol�: �arpt���m�z objenin bir UnitIdentity'si var m�?
            if (other.TryGetComponent<Soldier>(out var unitIdentity))
            {
                // Kendi kimlik bilgimiz ayarl� de�ilse devam etme (Hata korumas�)
                if (myIdentity == null)
                {
                    Debug.LogError("TargetDetector i�in gerekli olan myIdentity (kendi birim bilgisi) null!");
                    return;
                }

                // 3. KR�T�K HATA AYIKLAMA LOGU: Tak�m ID'lerini kar��la�t�r�r.
                // Bu log, blo�a girilip girilmedi�ini anlaman�n anahtar�d�r.
                Debug.Log($"[SERVER DETECTOR DEBUG] Kendi Tak�m: {myIdentity.TeamId.Value}, Tetikleyenin Tak�m�: {unitIdentity.TeamId.Value}. �sim: {other.name}");

                // 4. D��man Kontrol�: Tak�m ID'miz ile �arpt���m�z birimin Tak�m ID'si farkl�ysa (Yani d��mansa)
                if (myIdentity.TeamId.Value != unitIdentity.TeamId.Value)
                {
                    // D��man Tespit Edildi Loglar�
                    Debug.Log($"[SERVER DETECTOR] D��MAN TESP�T ED�LD�! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");

                    // Yak�nl�k, mesafe, �ncelik vb. KONTROL� OLMADAN hemen hedefi de�i�tir.
                    soldiersControllerNavMesh.GiveMeNewTarget(other.transform);

                    // Hedef Atama Logu
                    Debug.Log($"[SERVER DETECTOR] D��man birim ({other.name}) menzile girdi. Yeni hedef atand�.");
                }


            }

            if (other.name == "generalTransform1")
            {
                Debug.Log($"[SERVER DETECTOR] Engel ile �arp��ma tespit edildi: {other.name}");
                // Engel ile �arp��ma durumunda yap�lacak i�lemler burada
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);
            }

        }
        else {
            // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendi�ini bildirir.
            Debug.Log($"[SERVER DETECTOR local] trigger tetiklendi ({other.name}) -------MyTeamID" + myIdentity.TeamId.Value);

            // 2. Birim Kimli�i Kontrol�: �arpt���m�z objenin bir UnitIdentity'si var m�?
            if (other.TryGetComponent<Soldier>(out var unitIdentity))
            {
                // Kendi kimlik bilgimiz ayarl� de�ilse devam etme (Hata korumas�)
                if (myIdentity == null)
                {
                    Debug.LogError("TargetDetector  local i�in gerekli olan myIdentity (kendi birim bilgisi) null!");
                    return;
                }

                // 3. KR�T�K HATA AYIKLAMA LOGU: Tak�m ID'lerini kar��la�t�r�r.
                // Bu log, blo�a girilip girilmedi�ini anlaman�n anahtar�d�r.
                Debug.Log($"[SERVER DETECTOR DEBUG] local Kendi Tak�m: {myIdentity.TeamId.Value}, Tetikleyenin Tak�m�: {unitIdentity.TeamId.Value}. �sim: {other.name}");

                // 4. D��man Kontrol�: Tak�m ID'miz ile �arpt���m�z birimin Tak�m ID'si farkl�ysa (Yani d��mansa)
                if (myIdentity.TeamId.Value != unitIdentity.TeamId.Value)
                {
                    // D��man Tespit Edildi Loglar�
                    Debug.Log($"[SERVER DETECTOR] local D��MAN TESP�T ED�LD�! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");

                    // Yak�nl�k, mesafe, �ncelik vb. KONTROL� OLMADAN hemen hedefi de�i�tir.
                    soldiersControllerNavMesh.GiveMeNewTarget(other.transform);

                    // Hedef Atama Logu
                    Debug.Log($"[SERVER DETECTOR]local D��man birim ({other.name}) menzile girdi. Yeni hedef atand�.");
                }


            }

            if (other.name == "generalTransform1")
            {
                Debug.Log($"[SERVER DETECTOR] local Engel ile �arp��ma tespit edildi: {other.name}");
                // Engel ile �arp��ma durumunda yap�lacak i�lemler burada
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);
            }
        }

       

       
    }
}
