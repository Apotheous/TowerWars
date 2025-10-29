using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class Soldier : NetworkBehaviour, IDamageable
{
    public Player_Game_Mode_Manager.PlayerAge age;

    [SerializeField] private float MaxHealth = 100f;

    private NetworkVariable<float> myHealth = new NetworkVariable<float>(
        100f,  // varsay�lan de�er
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

    // Bu de�i�keni 3 script'e de ekle (Soldier, SoldiersControllerNavMesh, SoldiersAttackController)
    [SerializeField] private Animator modelAnimator;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable'lar�n senkronizasyonu tamamland���nda bu metot �al���r.

        // 2. Sadece Server �zerinde �al��acak mant�k
        if (IsServer)
        {
            myHealth.Value = MaxHealth;

            gameObject.name = TeamId.Value.ToString();


            // 1. TeamId'yi G�VENL� bir �ekilde al ve sakla
            var soldiersControllerNavMesh = GetComponent<SoldiersControllerNavMesh>();
            if (soldiersControllerNavMesh != null)
            {
                // Coroutine'i bu �ekilde ba�latmal�s�n:
                StartCoroutine(soldiersControllerNavMesh.FindTargetAndSetDestination());
            }
            else
            {
                Debug.LogError("Bu objenin �zerinde SoldiersControllerNavMesh component'i bulunamad�!", gameObject);
            }
        }
        // 3. T�m client'larda �al��acak mant�k (G�rsel g�ncellemeler vb.)
        myHealth.OnValueChanged += OnHealthChanged;
        OnHealthChanged(0, myHealth.Value); // UI'� ilk de�erle g�ncelle
        if (targetDetector != null)
        {
            targetDetector.WhenNetworkSpawn();
        }
        else
        {
            Debug.LogError("Bu objenin �zerinde TargetDetector component'i bulunamad�!", gameObject);
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
        TakeDamage(damageAmount); // interface metodunu �a��r
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
        // YALNIZCA SUNUCUDA �ALI�TIR!
        if (!IsServer) return;
      


        // 2. Collider'� kapat (�l� birime �arpmas�nlar/ate� etmesinler)
        // E�er ana objede bir Collider varsa:
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;


        // 3. �l�m animasyonunu tetikle
        if (modelAnimator != null)
        {
            modelAnimator.SetTrigger("Die");
            Debug.Log($"[SERVER] {gameObject.name} i�in Die animasyonu tetiklendi.");
        }
 
        // �len askerin d��man tak�m ID'sini bul
        int killingPlayerTeamId = (TeamId.Value == 1) ? 2 : 1;

        // Y�NET�C� ARACILI�IYLA D�REKT ER���M (O(1))
        PlayerSC enemyPlayerSC = OneVsOneModePlayerSCHolder.Instance.GetPlayerByTeamId(killingPlayerTeamId);

        if (enemyPlayerSC != null)
        {
            enemyPlayerSC.UpdateMyScrap(myPrizeScrap);
            enemyPlayerSC.UpdateExpPointIncrease(myPrizeExp);
            enemyPlayerSC.AddTempPoint();

            Debug.Log($"[Server] Manager: Team {killingPlayerTeamId} gained {myPrizeScrap} scrap.");
        }

        //GetComponent<NetworkObject>().Despawn();
        // YEN� KOD: Animasyonun bitmesi i�in bekle ve sonra Despawn et
        StartCoroutine(DespawnAfterDelay(2.5f)); // Animasyon s�rene g�re ayarla (�rn: 2.5 saniye)
    }


        // Bu yeni coroutine'i Soldier.cs s�n�f�n�n i�ine ekle
    private IEnumerator DespawnAfterDelay(float delay)
    {
        // Animasyonun oynamas� i�in belirlenen s�re kadar bekle
        yield return new WaitForSeconds(delay);

        // S�re dolduktan sonra objeyi network'ten kald�r
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
