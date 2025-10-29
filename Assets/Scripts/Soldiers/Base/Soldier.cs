using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class Soldier : NetworkBehaviour, IDamageable
{
    public Player_Game_Mode_Manager.PlayerAge age;

    [SerializeField] private float MaxHealth = 100f;

    private NetworkVariable<float> myHealth = new NetworkVariable<float>(
        100f,  // varsayýlan deðer
        NetworkVariableReadPermission.Everyone,  // herkes okuyabilir
        NetworkVariableWritePermission.Server    // sadece server yazabilir
    );

    public NetworkVariable<int> TeamId = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Unity Stuff")]
    public Image healthBar;

    [SerializeField] private float myCost;
    [SerializeField] private float myPrizeScrap;
    [SerializeField] private float myPrizeExp;



    [Header("My Comps")]
    [SerializeField] private SoldiersControllerNavMesh soldiersControllerNavMesh;
    [SerializeField] private TargetDetector targetDetector;

    // Bu deðiþkeni 3 script'e de ekle (Soldier, SoldiersControllerNavMesh, SoldiersAttackController)
    [SerializeField] private Animator modelAnimator;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable'larýn senkronizasyonu tamamlandýðýnda bu metot çalýþýr.

        // 2. Sadece Server üzerinde çalýþacak mantýk
        if (IsServer)
        {
            myHealth.Value = MaxHealth;

            gameObject.name = TeamId.Value.ToString();


            // 1. TeamId'yi GÜVENLÝ bir þekilde al ve sakla
            var soldiersControllerNavMesh = GetComponent<SoldiersControllerNavMesh>();
            if (soldiersControllerNavMesh != null)
            {
                // Coroutine'i bu þekilde baþlatmalýsýn:
                StartCoroutine(soldiersControllerNavMesh.FindTargetAndSetDestination());
            }
            else
            {
                Debug.LogError("Bu objenin üzerinde SoldiersControllerNavMesh component'i bulunamadý!", gameObject);
            }
        }
        // 3. Tüm client'larda çalýþacak mantýk (Görsel güncellemeler vb.)
        myHealth.OnValueChanged += OnHealthChanged;
        OnHealthChanged(0, myHealth.Value); // UI'ý ilk deðerle güncelle
        if (targetDetector != null)
        {
            targetDetector.WhenNetworkSpawn();
        }
        else
        {
            Debug.LogError("Bu objenin üzerinde TargetDetector component'i bulunamadý!", gameObject);
        }

    }


    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthBar != null)
            healthBar.fillAmount = newValue / MaxHealth;
    }



    // === IDamageable implementasyonu ===
    [ServerRpc]
    public void TakeDamageServerRpc(float damageAmount)
    {
        TakeDamage(damageAmount); // interface metodunu çaðýr
    }

    public void TakeDamage(float damageAmount)
    {
     
        if (myHealth.Value <= 0) return;

        myHealth.Value -= damageAmount;
        if (myHealth.Value <= 0)
        {
            Die();
        }
       
    }

    public void Die()
    {
        // YALNIZCA SUNUCUDA ÇALIÞTIR!
        if (!IsServer) return;
      


        // 2. Collider'ý kapat (ölü birime çarpmasýnlar/ateþ etmesinler)
        // Eðer ana objede bir Collider varsa:
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;


        // 3. Ölüm animasyonunu tetikle
        if (modelAnimator != null)
        {
            modelAnimator.SetTrigger("Die");
            Debug.Log($"[SERVER] {gameObject.name} için Die animasyonu tetiklendi.");
        }
 
        // Ölen askerin düþman takým ID'sini bul
        int killingPlayerTeamId = (TeamId.Value == 1) ? 2 : 1;

        // YÖNETÝCÝ ARACILIÐIYLA DÝREKT ERÝÞÝM (O(1))
        PlayerSC enemyPlayerSC = OneVsOneModePlayerSCHolder.Instance.GetPlayerByTeamId(killingPlayerTeamId);

        if (enemyPlayerSC != null)
        {
            enemyPlayerSC.UpdateMyScrap(myPrizeScrap);
            enemyPlayerSC.UpdateExpPointIncrease(myPrizeExp);
            enemyPlayerSC.AddTempPoint();

            Debug.Log($"[Server] Manager: Team {killingPlayerTeamId} gained {myPrizeScrap} scrap.");
        }

        //GetComponent<NetworkObject>().Despawn();
        // YENÝ KOD: Animasyonun bitmesi için bekle ve sonra Despawn et
        StartCoroutine(DespawnAfterDelay(2.5f)); // Animasyon sürene göre ayarla (örn: 2.5 saniye)
    }


        // Bu yeni coroutine'i Soldier.cs sýnýfýnýn içine ekle
    private IEnumerator DespawnAfterDelay(float delay)
    {
        // Animasyonun oynamasý için belirlenen süre kadar bekle
        yield return new WaitForSeconds(delay);

        // Süre dolduktan sonra objeyi network'ten kaldýr
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
