using Unity.Netcode;
using UnityEngine;

public class TargetDetector : NetworkBehaviour
{
    [SerializeField] private SoldiersControllerNavMesh controllerNavMesh;
    [SerializeField] private SoldiersAttackController controllerAttack;
    [SerializeField] private Soldier soldier; // Kendi takým bilgimizi tutan referans
    [SerializeField] Transform target; // Þu an kullanýlmýyor, ama gelecekte hedefi tutmak için kullanýlabilir.

    public void WhenNetworkSpawn()
    {
        Debug.Log($"[SERVER DETECTOR] Baþladý OnNetworkSpawn");
        // SoldiersControllerNavMesh'e ulaþmanýn en saðlam yolu:
        if (controllerNavMesh == null)
        {
            controllerNavMesh = GetComponentInParent<SoldiersControllerNavMesh>();
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh çekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersControllerNavMesh zaten atanmýþ");
        }
        if (controllerAttack == null)
        {
            controllerAttack = GetComponentInParent<SoldiersAttackController>();
            Debug.Log($"[SERVER DETECTOR] SoldiersAttackController çekildi");
        }else
        {
            Debug.Log($"[SERVER DETECTOR] SoldiersAttackController zaten atanmýþ");
        }

        // Kendi UnitIdentity'mizi bul (takým bilgisi için kritik).
        soldier = GetComponentInParent<Soldier>();
        Debug.Log($"[SERVER DETECTOR] UnitIdentity çekildi MyTeamID ===" + soldier.TeamId.Value +"++++");
        if (soldier == null)
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
            Debug.Log($"[DETECTOR - FÝZÝK] Tetiklenme: {potentialTargetParent.name} (Parent) | Kendi Takým ID: {soldier.TeamId.Value}");

            // 2. YALNIZCA SUNUCUDA ÇALIÞACAK KRÝTÝK OYUN MANTIÐI
            // Bu birim Sunucu tarafýndan kontrol ediliyorsa, hedefleme kararlarýný al.
            if (IsServer) // IsServer kontrolünü ekleyelim
            {
                // Hedef bileþenini almayý dene
                if (potentialTargetParent.TryGetComponent<Soldier>(out var unitIdentity))
                {
                    // Kendi birim bilgimizin varlýðýný kontrol et (Hata korumasý)
                    if (soldier == null)
                    {
                        Debug.LogError("[SERVER DETECTOR] Kendi kimlik bilgisi (myIdentity) bulunamadý!");
                        return;
                    }

                    // Takým ID'lerini karþýlaþtýr ve logla
                    var myTeam = soldier.TeamId.Value;
                    var otherTeam = unitIdentity.TeamId.Value;
                    Debug.Log($"[SERVER DETECTOR KRÝTÝK ANALÝZ] Kendi: {myTeam}, Diðer: {otherTeam} | Ýsim: {potentialTargetParent.name}");

                    // 3. DÜÞMAN KONTROLÜ
                    if (myTeam != otherTeam)
                    {
                        // Düþman Tespit Edildi Logu
                        Debug.Log($"[SERVER DETECTOR] DÜÞMAN TESPÝT EDÝLDÝ! Hedef: {potentialTargetParent.name}");

                        // Hedefle ilgili kararlarý (durma, hedef atama) SADECE SUNUCU alýr.
                        //controllerNavMesh.StopUnit();
                        controllerNavMesh.GiveMeNewTarget(potentialTargetParent);
                        controllerAttack.StartAttacking(potentialTargetParent);

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
            Debug.Log($"[DETECTOR - FÝZÝK] Tetiklenme: {other.transform.name} (Kendi) | Kendi Takým ID: {soldier.TeamId.Value}");
        }
        // Genel Tetiklenme Logu: Hangi nesne olursa olsun tetiklendiðini bildirir.
    }
}
