using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private Soldier myIdentity; // Kendi takým bilgimizi tutan referans
    [SerializeField] Transform target; // Þu an kullanýlmýyor, ama gelecekte hedefi tutmak için kullanýlabilir.

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

        // 1. GENEL LOG: Kimin neye çarptýðýný her iki tarafta da yazdýr.
        // Bu, debug için önemlidir, tetikleyicinin çalýþýp çalýþmadýðýný gösterir.
        Transform potentialTargetParent = other.transform.parent;

        if (potentialTargetParent != null)
        {
            // Tetiklenme Logu: Parent nesnenin adýný yazdýrýr.
            Debug.Log($"[DETECTOR - FÝZÝK] Tetiklenme: {potentialTargetParent.name} (Parent) | Kendi Takým ID: {myIdentity.TeamId.Value}");

            // 2. YALNIZCA SUNUCUDA ÇALIÞACAK KRÝTÝK OYUN MANTIÐI
            // Bu birim Sunucu tarafýndan kontrol ediliyorsa, hedefleme kararlarýný al.
            if (IsServer) // IsServer kontrolünü ekleyelim
            {
                // Hedef bileþenini almayý dene
                if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
                {
                    // Kendi birim bilgimizin varlýðýný kontrol et (Hata korumasý)
                    if (myIdentity == null)
                    {
                        Debug.LogError("[SERVER DETECTOR] Kendi kimlik bilgisi (myIdentity) bulunamadý!");
                        return;
                    }

                    // Takým ID'lerini karþýlaþtýr ve logla
                    var myTeam = myIdentity.TeamId.Value;
                    var otherTeam = unitIdentity.TeamId.Value;
                    Debug.Log($"[SERVER DETECTOR KRÝTÝK ANALÝZ] Kendi: {myTeam}, Diðer: {otherTeam} | Ýsim: {potentialTargetParent.name}");

                    // 3. DÜÞMAN KONTROLÜ
                    if (myTeam != otherTeam)
                    {
                        // Düþman Tespit Edildi Logu
                        Debug.Log($"[SERVER DETECTOR] DÜÞMAN TESPÝT EDÝLDÝ! Hedef: {potentialTargetParent.name}");

                        // Hedefle ilgili kararlarý (durma, hedef atama) SADECE SUNUCU alýr.
                        soldiersControllerNavMesh.StopUnit();
                        // soldiersControllerNavMesh.GiveMeNewTarget(potentialTargetParent); 

                        // Að üzerindeki tüm istemcilere birimin durduðu bilgisini Network/RPC ile göndermeniz gerekebilir.
                        // Bu, NavMesh Agent'ýn durma iþleminin yerel olarak görselleþtirilmesini saðlar.

                        Debug.Log($"[SERVER DETECTOR] Durma emri verildi ve yeni hedef kararý alýnýyor.");
                    }
                }
            }
        }
        else
        {
            // Parent'ý olmayan objelerin (örneðin yerdeki bir powerup) tetiklenme logu
            Debug.Log($"[DETECTOR - FÝZÝK] Tetiklenme: {other.transform.name} (Kendi) | Kendi Takým ID: {myIdentity.TeamId.Value}");
        }
        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendiðini bildirir.

        if (other.transform.parent != null)
        {
            target = other.transform.parent;
            Debug.Log($"[SERVER DETECTOR] trigger tetiklendi Panetýnýn ({target.name}) -------MyTeamID" + myIdentity.TeamId.Value);
            // 2. Birim Kimliði Kontrolü: Çarptýðýmýz objenin bir UnitIdentity'si var mý?
            if (target.TryGetComponent<Soldier>(out var unitIdentity))
            {

                Debug.LogError("TargetDetector için gerekli olan myIdentity (kendi birim bilgisi) çekildi");
                // Kendi kimlik bilgimiz ayarlý deðilse devam etme (Hata korumasý)
                if (myIdentity == null)
                {
                    Debug.LogError("TargetDetector için gerekli olan myIdentity (kendi birim bilgisi) null!");
                    return;
                }
                // YENÝ KRÝTÝK KONTROL - TeamID'leri netleþtir
                var myTeam = myIdentity.TeamId.Value;
                var otherTeam = unitIdentity.TeamId.Value;
                // 3. KRÝTÝK HATA AYIKLAMA LOGU: Takým ID'lerini karþýlaþtýrýr.
                // Bu log, bloða girilip girilmediðini anlamanýn anahtarýdýr.

                Debug.Log($"[SERVER DETECTOR KRÝTÝK ANALÝZ] Kendi: {myTeam}, Diðer: {otherTeam} + Ýsim: {target.name}");
                // 4. Düþman Kontrolü: Takým ID'miz ile çarptýðýmýz birimin Takým ID'si farklýysa (Yani düþmansa)
                if (myIdentity.TeamId.Value != unitIdentity.TeamId.Value)
                {
                    // Düþman Tespit Edildi Loglarý
                    Debug.Log($"[SERVER DETECTOR] DÜÞMAN TESPÝT EDÝLDÝ! myIdentity=={myIdentity.TeamId.Value} other Identity =={unitIdentity.TeamId.Value}---");

                    // Yakýnlýk, mesafe, öncelik vb. KONTROLÜ OLMADAN hemen hedefi deðiþtir.
                    soldiersControllerNavMesh.StopUnit();
                    //soldiersControllerNavMesh.GiveMeNewTarget(target);

                    // Hedef Atama Logu
                    Debug.Log($"[SERVER DETECTOR] Düþman birim ({target}) menzile girdi. Yeni hedef atandý.");
                }

            }
        }
        else
        {
            Debug.Log($"[SERVER DETECTOR] trigger tetiklendi kendi ({other.transform.name}) -------MyTeamID" + myIdentity.TeamId.Value);
        }
    }
}
