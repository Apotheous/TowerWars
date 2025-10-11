using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class DevSingletonTransform : NetworkBehaviour
{
    public static DevSingletonTransform instance;

    // NOT: player1 ve player2 referanslar� SADECE SUNUCU TARAFINDA DOLDURULACAK! 
    // Client'lar bu referanslar� bilemez. E�er client'larda da gerekirse, 
    // NetworkVariable<ulong> kullanarak NetworkObjectId'lerini senkronize etmeliyiz.
    public PlayerSC player1, player2;

    public Transform player1Transform, player2Transform, publicTransform;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // E�ER DontDestroyOnLoad kullan�yorsan�z, buraya ekleyin.
        }
        else
        {
            Destroy(gameObject); // �ift olu�umu engelle
        }
    }

    void Start()
    {
        // Start'ta NetworkManager'�n varl���na bak�lmaks�z�n coroutine'i ba�lat.
        StartCoroutine(SetupPlayersDelayed());
    }

    // NetworkBehaviour'dan miras ald���m�z i�in OnNetworkSpawn'u kullanmak daha g�venlidir.
    public override void OnNetworkSpawn()
    {
        // E�er sunucuysak, PlayerSC referanslar�n� atamak i�in haz�rl�k yapabiliriz.
        if (IsServer)
        {
            // E�er Start() coroutine'i �al��m�yorsa buradan da �a��rabiliriz.
            // Ama Start'taki coroutine NetworkManager'�n haz�r olmas�n� bekledi�i i�in Start() yeterli.
        }
    }


    IEnumerator SetupPlayersDelayed()
    {
        // NetworkManager'�n haz�r olmas�n� beklemek
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        // Oyuncular�n spawn edilmesini ve TeamId'lerinin atanmas�n� beklemek i�in 1.5 saniye.
        yield return new WaitForSeconds(1.5f);

        // Sadece SUNUCU bu atamay� yapar
        if (NetworkManager.Singleton.IsServer)
        {
            AssignPlayersByTeamId();

            // Atama yap�ld�ktan hemen sonra yerle�tirme i�lemini T�M CLIENT'lara g�nder.
            // Bu, client'lar�n kendi yerel objelerini hareket ettirmelerini sa�layacak.
            RequestPlacePlayersClientRpc();
        }
        // E�er client isek, sadece yerle�tirme RPC'sini bekleyece�iz.
        // Client'lar kendi ba�lar�na bir �ey yapmaz.
    }

    /// <summary>
    /// Oyundaki t�m oyuncular� TeamId'lerine g�re player1 ve player2'ye atar (SADECE SUNUCU TARAFINDA �ALI�IR)
    /// </summary>
    private void AssignPlayersByTeamId()
    {
        // Sadece Sunucu'da �al��t���ndan emin ol.
        if (!IsServer) return;

        var allPlayers = NetworkManager.Singleton.ConnectedClientsList
                             .Select(c => c.PlayerObject?.GetComponent<PlayerSC>())
                             .Where(p => p != null)
                             .ToList();

        player1 = allPlayers.FirstOrDefault(p => p.TeamId.Value == 1);
        player2 = allPlayers.FirstOrDefault(p => p.TeamId.Value == 2);

        Debug.Log($"[DevSingletonTransform-Server] Player 1 (Team 1) atand�: {(player1 != null ? player1.OwnerClientId.ToString() : "YOK")}");
        Debug.Log($"[DevSingletonTransform-Server] Player 2 (Team 2) atand�: {(player2 != null ? player2.OwnerClientId.ToString() : "YOK")}");
    }

    // --- YERLE�T�RME KISMI ---

    // Sunucudan t�m client'lara yerle�tirme metodunu �a��rmas�n� isteyen RPC.
    [ClientRpc]
    private void RequestPlacePlayersClientRpc()
    {
        // RPC �a�r�s� geldikten sonra yerle�tirmeyi hemen yap.
        // Bu metod hem Server'da (Host) hem de t�m Client'larda �al��acakt�r.
        PlaceLocalPlayer();
    }


    /// <summary>
    /// Her client, kendi yerel oyuncusunu do�ru spawn noktas�na yerle�tirir.
    /// Bu metod, t�m client'larda �a�r�lmal�d�r (Host da dahil).
    /// </summary>
    public void PlaceLocalPlayer()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayer == null)
        {
            Debug.LogError("[DevSingletonTransform] Local Player Object bulunamad�!");
            return;
        }

        var playerSC = localPlayer.GetComponent<PlayerSC>();

        if (playerSC != null)
        {
            // NetworkVariable'�n de�erinin senkronize olmas�n� bekle.
            if (playerSC.TeamId.Value == 1)
            {
                // Local Player'�n Transform'unu de�i�tirmek i�in yetkimiz var.
                localPlayer.transform.SetPositionAndRotation(player1Transform.position, player1Transform.rotation);
                Debug.Log($"[Client-{NetworkManager.Singleton.LocalClientId}] Team 1 (Player 1) noktas�na yerle�tirildi.");
            }
            else if (playerSC.TeamId.Value == 2)
            {
                localPlayer.transform.SetPositionAndRotation(player2Transform.position, player2Transform.rotation);
                Debug.Log($"[Client-{NetworkManager.Singleton.LocalClientId}] Team 2 (Player 2) noktas�na yerle�tirildi.");
            }
        }
        else
        {
            Debug.LogError("[DevSingletonTransform] Local PlayerSC component'i bulunamad�!");
        }
    }
}
