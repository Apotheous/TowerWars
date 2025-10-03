using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    private UnitIdentity myIdentity; // Kendi tak�m bilgimizi tutan referans

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
        myIdentity = GetComponentInParent<UnitIdentity>();
        Debug.Log($"[SERVER DETECTOR] UnitIdentity �ekildi MyTeamID ===" + myIdentity.TeamId.Value +"++++");
        if (myIdentity == null)
        {
            Debug.LogError("TargetDetector i�in gerekli olan UnitIdentity bulunamad�!");

        }

    }

    private void OnTriggerEnter(Collider other)
    {
       
        // 1. Sunucu Kontrol�: Hareket kararlar� sadece sunucuda al�n�r.
        if (!soldiersControllerNavMesh.IsServer) return;

        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendi�ini bildirir.
        Debug.Log($"[SERVER DETECTOR] trigger tetiklendi ({other.name}) -------");

        // 2. Birim Kimli�i Kontrol�: �arpt���m�z objenin bir UnitIdentity'si var m�?
        if (other.TryGetComponent<UnitIdentity>(out var unitIdentity))
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
            if (gameObject.name != other.gameObject.name)
            {
                // D��man Tespit Edildi Loglar�
                Debug.Log($"[SERVER DETECTOR] D��MAN TESP�T ED�LD�! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");
                Debug.Log($"[SERVER DETECTOR] D��MAN TESP�T ED�LD�! name=={gameObject.name} other Identity =={gameObject.name}---");

                // Yak�nl�k, mesafe, �ncelik vb. KONTROL� OLMADAN hemen hedefi de�i�tir.
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);

                // Hedef Atama Logu
                Debug.Log($"[SERVER DETECTOR] D��man birim ({other.name}) menzile girdi. Yeni hedef atand�.");
                Debug.Log($"[SERVER DETECTOR] D��man birim ({other.name}) menzile girdi. Yeni hedef atand�.");
            }
    
        }
    }
}
