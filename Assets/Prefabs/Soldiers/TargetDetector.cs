using Unity.Netcode;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    private SoldiersControllerNavMesh soldiersControllerNavMesh;
    private Soldier myIdentity; // Kendi takým bilgimizi tutan referans

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
        myIdentity = GetComponentInParent<Soldier>();
        Debug.Log($"[SERVER DETECTOR] UnitIdentity çekildi MyTeamID ===" + myIdentity.TeamId.Value +"++++");
        if (myIdentity == null)
        {
            Debug.LogError("TargetDetector için gerekli olan UnitIdentity bulunamadý!");

        }

    }

    private void OnTriggerEnter(Collider other)
    {

        // 1. Sunucu Kontrolü: Hareket kararlarý sadece sunucuda alýnýr.
        if (soldiersControllerNavMesh.IsServer)
        {

            // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendiðini bildirir.
            Debug.Log($"[SERVER DETECTOR] trigger tetiklendi ({other.name}) -------MyTeamID" + myIdentity.TeamId.Value);

            // 2. Birim Kimliði Kontrolü: Çarptýðýmýz objenin bir UnitIdentity'si var mý?
            if (other.TryGetComponent<Soldier>(out var unitIdentity))
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


            }

            if (other.name == "generalTransform1")
            {
                Debug.Log($"[SERVER DETECTOR] Engel ile çarpýþma tespit edildi: {other.name}");
                // Engel ile çarpýþma durumunda yapýlacak iþlemler burada
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);
            }

        }
        else {
            // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendiðini bildirir.
            Debug.Log($"[SERVER DETECTOR local] trigger tetiklendi ({other.name}) -------MyTeamID" + myIdentity.TeamId.Value);

            // 2. Birim Kimliði Kontrolü: Çarptýðýmýz objenin bir UnitIdentity'si var mý?
            if (other.TryGetComponent<Soldier>(out var unitIdentity))
            {
                // Kendi kimlik bilgimiz ayarlý deðilse devam etme (Hata korumasý)
                if (myIdentity == null)
                {
                    Debug.LogError("TargetDetector  local için gerekli olan myIdentity (kendi birim bilgisi) null!");
                    return;
                }

                // 3. KRÝTÝK HATA AYIKLAMA LOGU: Takým ID'lerini karþýlaþtýrýr.
                // Bu log, bloða girilip girilmediðini anlamanýn anahtarýdýr.
                Debug.Log($"[SERVER DETECTOR DEBUG] local Kendi Takým: {myIdentity.TeamId.Value}, Tetikleyenin Takýmý: {unitIdentity.TeamId.Value}. Ýsim: {other.name}");

                // 4. Düþman Kontrolü: Takým ID'miz ile çarptýðýmýz birimin Takým ID'si farklýysa (Yani düþmansa)
                if (myIdentity.TeamId.Value != unitIdentity.TeamId.Value)
                {
                    // Düþman Tespit Edildi Loglarý
                    Debug.Log($"[SERVER DETECTOR] local DÜÞMAN TESPÝT EDÝLDÝ! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");

                    // Yakýnlýk, mesafe, öncelik vb. KONTROLÜ OLMADAN hemen hedefi deðiþtir.
                    soldiersControllerNavMesh.GiveMeNewTarget(other.transform);

                    // Hedef Atama Logu
                    Debug.Log($"[SERVER DETECTOR]local Düþman birim ({other.name}) menzile girdi. Yeni hedef atandý.");
                }


            }

            if (other.name == "generalTransform1")
            {
                Debug.Log($"[SERVER DETECTOR] local Engel ile çarpýþma tespit edildi: {other.name}");
                // Engel ile çarpýþma durumunda yapýlacak iþlemler burada
                soldiersControllerNavMesh.GiveMeNewTarget(other.transform);
            }
        }

       

       
    }
}
