using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    private UnitIdentity myIdentity; // Kendi takým bilgimizi tutan referans

    public void WhenNetworkSpawn()
    {
        Debug.Log($"[SERVER DETECTOR] Baþladý OnNetworkSpawn");
        // SoldiersControllerNavMesh'e ulaþmanýn en saðlam yolu:
        if (soldiersControllerNavMesh == null)
        {
            soldiersControllerNavMesh = GetComponentInParent<SoldiersControllerNavMesh>();
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh çekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh zaten atanmýþ");
        }

        // Kendi UnitIdentity'mizi bul (takým bilgisi için kritik).
        myIdentity = GetComponentInParent<UnitIdentity>();
        Debug.Log($"[SERVER DETECTOR] UnitIdentity çekildi MyTeamID ===" + myIdentity.TeamId.Value +"++++");
        if (myIdentity == null)
        {
            Debug.LogError("TargetDetector için gerekli olan UnitIdentity bulunamadý!");

        }

    }

    private void OnTriggerEnter(Collider other)
    {
       
        // 1. Sunucu Kontrolü: Hareket kararlarý sadece sunucuda alýnýr.
        if (!soldiersControllerNavMesh.IsServer) return;

        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendiðini bildirir.
        Debug.Log($"[SERVER DETECTOR] trigger tetiklendi ({other.name}) -------");

        // 2. Birim Kimliði Kontrolü: Çarptýðýmýz objenin bir UnitIdentity'si var mý?
        if (other.TryGetComponent<UnitIdentity>(out var unitIdentity))
        {
            // Kendi kimlik bilgimiz ayarlý deðilse devam etme (Hata korumasý)
            if (myIdentity == null)
            {
                Debug.LogError("TargetDetector için gerekli olan myIdentity (kendi birim bilgisi) null!");
                return;
            }

            // 3. KRÝTÝK HATA AYIKLAMA LOGU: Takým ID'lerini karþýlaþtýrýr.
            // Bu log, bloða girilip girilmediðini anlamanýn anahtarýdýr.
            Debug.Log($"[SERVER DETECTOR DEBUG] Kendi Takým: {myIdentity.TeamId.Value}, Tetikleyenin Takýmý: {unitIdentity.TeamId.Value}. Ýsim: {other.name}");

            // 4. Düþman Kontrolü: Takým ID'miz ile çarptýðýmýz birimin Takým ID'si farklýysa (Yani düþmansa)
            if (myIdentity.TeamId.Value != unitIdentity.TeamId.Value)
            {
                // Düþman Tespit Edildi Loglarý
                Debug.Log($"[SERVER DETECTOR] DÜÞMAN TESPÝT EDÝLDÝ! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");

                // Yakýnlýk, mesafe, öncelik vb. KONTROLÜ OLMADAN hemen hedefi deðiþtir.
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);

                // Hedef Atama Logu
                Debug.Log($"[SERVER DETECTOR] Düþman birim ({other.name}) menzile girdi. Yeni hedef atandý.");
            }
            if (gameObject.name != other.gameObject.name)
            {
                // Düþman Tespit Edildi Loglarý
                Debug.Log($"[SERVER DETECTOR] DÜÞMAN TESPÝT EDÝLDÝ! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");
                Debug.Log($"[SERVER DETECTOR] DÜÞMAN TESPÝT EDÝLDÝ! name=={gameObject.name} other Identity =={gameObject.name}---");

                // Yakýnlýk, mesafe, öncelik vb. KONTROLÜ OLMADAN hemen hedefi deðiþtir.
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);

                // Hedef Atama Logu
                Debug.Log($"[SERVER DETECTOR] Düþman birim ({other.name}) menzile girdi. Yeni hedef atandý.");
                Debug.Log($"[SERVER DETECTOR] Düþman birim ({other.name}) menzile girdi. Yeni hedef atandý.");
            }
    
        }
    }
}
