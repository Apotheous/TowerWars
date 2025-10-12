using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OneVsOneModePlayerSCHolder : NetworkBehaviour
{
    // Singleton eriþimi için Instance
    public static OneVsOneModePlayerSCHolder Instance { get; private set; }

    // TeamId -> PlayerSC objesini tutan sunucu tarafý sözlüðü
    private readonly Dictionary<int, PlayerSC> PlayersByTeamId = new Dictionary<int, PlayerSC>();

    // Baþka bir instance olup olmadýðýný kontrol eden deðiþken
    private bool hasInitialized = false;

    // YENÝ: Awake'te Singleton kontrolü (Að baþlamadan önce çalýþýr)
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SCHolder-Awake] Zaten bir Instance var. Bu kopya ({gameObject.name}) yok ediliyor.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log($"[SCHolder-Awake] Singleton Instance ayarlandý: {gameObject.name}");
    }

    public override void OnNetworkSpawn()
    {
        // Sadece bir kere baþlatýldýðýndan emin ol
        if (hasInitialized) return;

        // Að durumunu logla
        if (IsServer)
        {
            Debug.Log("[SCHolder-Spawn] **SUNUCU/HOST:** Aðda doðdu. DontDestroyOnLoad ayarlanýyor.");
        }
        else
        {
            Debug.Log("[SCHolder-Spawn] **CLIENT:** Aðda doðdu.");
        }

        // Network objesi aða katýldýðý anda DontDestroyOnLoad çaðrýlýr.
        DontDestroyOnLoad(gameObject);

        Debug.Log("[SCHolder-Spawn] Manager objesi DontDestroyOnLoad olarak ayarlandý.");

        hasInitialized = true;
    }

    public override void OnNetworkDespawn()
    {
        // Aðdan kaldýrýldýðýnda Instance'ý temizle.
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[SCHolder-Despawn] Instance temizlendi.");
        }
        // YALNIZCA SUNUCUDA: Player listesini temizle
        if (IsServer)
        {
            PlayersByTeamId.Clear();
            Debug.Log("[SCHolder-Despawn] SUNUCU: Oyuncu sözlüðü temizlendi.");
        }
    }

    // --- Metotlar ---

    /// <summary>
    /// PlayerSC objesini sözlüðe kaydeder. Sadece SUNUCUDA çalýþýr.
    /// </summary>
    public void RegisterPlayer(int teamId, PlayerSC player)
    {
        if (IsServer)
        {
            PlayersByTeamId[teamId] = player;
            Debug.Log($"[SCHolder-Server-Register] **BAÞARILI:** Team {teamId} PlayerSC objesi ({player.gameObject.name}) sözlüðe kaydedildi. Toplam oyuncu: {PlayersByTeamId.Count}");
        }
        else
        {
            // Hata ayýklama için önemli: Client'lar bu kodu çalýþtýrmamalý!
            Debug.LogWarning($"[SCHolder-Client-Register] **UYARI:** Team {teamId} PlayerSC kaydý CLIENT tarafýnda denendi ve engellendi. Bu iþlem sadece SUNUCU tarafýndan yapýlmalýdýr.");
        }
    }

    /// <summary>
    /// TeamId'ye göre PlayerSC objesini döndürür.
    /// </summary>
    public PlayerSC GetPlayerByTeamId(int teamId)
    {
        if (PlayersByTeamId.TryGetValue(teamId, out PlayerSC player))
        {
            // Bu sorgu sýk sýk çalýþacaðý için sadece baþarýyý loglayalým.
            // Debug.Log($"[SCHolder-Query] Team {teamId} PlayerSC objesi bulundu.");
            return player;
        }

        // Hata durumunda detaylý loglama yapalým
        if (IsServer)
        {
            Debug.LogError($"[SCHolder-Query] SUNUCU: Team {teamId} için PlayerSC bulunamadý! Sözlük boyutu: {PlayersByTeamId.Count}.");
        }
        else
        {
            // Client'ta Manager'ýn olmasý sorun deðil, ama PlayerSC'ye eriþilememesi sorun.
            Debug.LogError($"[SCHolder-Query] CLIENT: Team {teamId} için PlayerSC bulunamadý! Sözlük, Network'ten senkronize edilmeli.");
        }

        return null;
    }
}
