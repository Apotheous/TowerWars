using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OneVsOneModePlayerSCHolder : NetworkBehaviour
{
    // Singleton eri�imi i�in Instance
    public static OneVsOneModePlayerSCHolder Instance { get; private set; }

    // TeamId -> PlayerSC objesini tutan sunucu taraf� s�zl���
    private readonly Dictionary<int, PlayerSC> PlayersByTeamId = new Dictionary<int, PlayerSC>();

    // Ba�ka bir instance olup olmad���n� kontrol eden de�i�ken
    private bool hasInitialized = false;

    // YEN�: Awake'te Singleton kontrol� (A� ba�lamadan �nce �al���r)
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SCHolder-Awake] Zaten bir Instance var. Bu kopya ({gameObject.name}) yok ediliyor.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log($"[SCHolder-Awake] Singleton Instance ayarland�: {gameObject.name}");
    }

    public override void OnNetworkSpawn()
    {
        // Sadece bir kere ba�lat�ld���ndan emin ol
        if (hasInitialized) return;

        // A� durumunu logla
        if (IsServer)
        {
            Debug.Log("[SCHolder-Spawn] **SUNUCU/HOST:** A�da do�du. DontDestroyOnLoad ayarlan�yor.");
        }
        else
        {
            Debug.Log("[SCHolder-Spawn] **CLIENT:** A�da do�du.");
        }

        // Network objesi a�a kat�ld��� anda DontDestroyOnLoad �a�r�l�r.
        DontDestroyOnLoad(gameObject);

        Debug.Log("[SCHolder-Spawn] Manager objesi DontDestroyOnLoad olarak ayarland�.");

        hasInitialized = true;
    }

    public override void OnNetworkDespawn()
    {
        // A�dan kald�r�ld���nda Instance'� temizle.
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[SCHolder-Despawn] Instance temizlendi.");
        }
        // YALNIZCA SUNUCUDA: Player listesini temizle
        if (IsServer)
        {
            PlayersByTeamId.Clear();
            Debug.Log("[SCHolder-Despawn] SUNUCU: Oyuncu s�zl��� temizlendi.");
        }
    }

    // --- Metotlar ---

    /// <summary>
    /// PlayerSC objesini s�zl��e kaydeder. Sadece SUNUCUDA �al���r.
    /// </summary>
    public void RegisterPlayer(int teamId, PlayerSC player)
    {
        if (IsServer)
        {
            PlayersByTeamId[teamId] = player;
            Debug.Log($"[SCHolder-Server-Register] **BA�ARILI:** Team {teamId} PlayerSC objesi ({player.gameObject.name}) s�zl��e kaydedildi. Toplam oyuncu: {PlayersByTeamId.Count}");
        }
        else
        {
            // Hata ay�klama i�in �nemli: Client'lar bu kodu �al��t�rmamal�!
            Debug.LogWarning($"[SCHolder-Client-Register] **UYARI:** Team {teamId} PlayerSC kayd� CLIENT taraf�nda denendi ve engellendi. Bu i�lem sadece SUNUCU taraf�ndan yap�lmal�d�r.");
        }
    }

    /// <summary>
    /// TeamId'ye g�re PlayerSC objesini d�nd�r�r.
    /// </summary>
    public PlayerSC GetPlayerByTeamId(int teamId)
    {
        if (PlayersByTeamId.TryGetValue(teamId, out PlayerSC player))
        {
            // Bu sorgu s�k s�k �al��aca�� i�in sadece ba�ar�y� loglayal�m.
            // Debug.Log($"[SCHolder-Query] Team {teamId} PlayerSC objesi bulundu.");
            return player;
        }

        // Hata durumunda detayl� loglama yapal�m
        if (IsServer)
        {
            Debug.LogError($"[SCHolder-Query] SUNUCU: Team {teamId} i�in PlayerSC bulunamad�! S�zl�k boyutu: {PlayersByTeamId.Count}.");
        }
        else
        {
            // Client'ta Manager'�n olmas� sorun de�il, ama PlayerSC'ye eri�ilememesi sorun.
            Debug.LogError($"[SCHolder-Query] CLIENT: Team {teamId} i�in PlayerSC bulunamad�! S�zl�k, Network'ten senkronize edilmeli.");
        }

        return null;
    }
}
