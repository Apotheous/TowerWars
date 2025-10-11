using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class DevSingletonTransform : NetworkBehaviour
{
    public static DevSingletonTransform instance;

    // NOT: player1 ve player2 referanslarý SADECE SUNUCU TARAFINDA DOLDURULACAK! 
    // Client'lar bu referanslarý bilemez. Eðer client'larda da gerekirse, 
    // NetworkVariable<ulong> kullanarak NetworkObjectId'lerini senkronize etmeliyiz.
    public PlayerSC player1, player2;

    public Transform player1Transform, player2Transform, publicTransform;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // EÐER DontDestroyOnLoad kullanýyorsanýz, buraya ekleyin.
        }
        else
        {
            Destroy(gameObject); // Çift oluþumu engelle
        }
    }

    void Start()
    {
        // Start'ta NetworkManager'ýn varlýðýna bakýlmaksýzýn coroutine'i baþlat.
        StartCoroutine(SetupPlayersDelayed());
    }

    // NetworkBehaviour'dan miras aldýðýmýz için OnNetworkSpawn'u kullanmak daha güvenlidir.
    public override void OnNetworkSpawn()
    {
        // Eðer sunucuysak, PlayerSC referanslarýný atamak için hazýrlýk yapabiliriz.
        if (IsServer)
        {
            // Eðer Start() coroutine'i çalýþmýyorsa buradan da çaðýrabiliriz.
            // Ama Start'taki coroutine NetworkManager'ýn hazýr olmasýný beklediði için Start() yeterli.
        }
    }


    IEnumerator SetupPlayersDelayed()
    {
        // NetworkManager'ýn hazýr olmasýný beklemek
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        // Oyuncularýn spawn edilmesini ve TeamId'lerinin atanmasýný beklemek için 1.5 saniye.
        yield return new WaitForSeconds(1.5f);

        // Sadece SUNUCU bu atamayý yapar
        if (NetworkManager.Singleton.IsServer)
        {
            AssignPlayersByTeamId();

            // Atama yapýldýktan hemen sonra yerleþtirme iþlemini TÜM CLIENT'lara gönder.
            // Bu, client'larýn kendi yerel objelerini hareket ettirmelerini saðlayacak.
            RequestPlacePlayersClientRpc();
        }
        // Eðer client isek, sadece yerleþtirme RPC'sini bekleyeceðiz.
        // Client'lar kendi baþlarýna bir þey yapmaz.
    }

    /// <summary>
    /// Oyundaki tüm oyuncularý TeamId'lerine göre player1 ve player2'ye atar (SADECE SUNUCU TARAFINDA ÇALIÞIR)
    /// </summary>
    private void AssignPlayersByTeamId()
    {
        // Sadece Sunucu'da çalýþtýðýndan emin ol.
        if (!IsServer) return;

        var allPlayers = NetworkManager.Singleton.ConnectedClientsList
                             .Select(c => c.PlayerObject?.GetComponent<PlayerSC>())
                             .Where(p => p != null)
                             .ToList();

        player1 = allPlayers.FirstOrDefault(p => p.TeamId.Value == 1);
        player2 = allPlayers.FirstOrDefault(p => p.TeamId.Value == 2);

        Debug.Log($"[DevSingletonTransform-Server] Player 1 (Team 1) atandý: {(player1 != null ? player1.OwnerClientId.ToString() : "YOK")}");
        Debug.Log($"[DevSingletonTransform-Server] Player 2 (Team 2) atandý: {(player2 != null ? player2.OwnerClientId.ToString() : "YOK")}");
    }

    // --- YERLEÞTÝRME KISMI ---

    // Sunucudan tüm client'lara yerleþtirme metodunu çaðýrmasýný isteyen RPC.
    [ClientRpc]
    private void RequestPlacePlayersClientRpc()
    {
        // RPC çaðrýsý geldikten sonra yerleþtirmeyi hemen yap.
        // Bu metod hem Server'da (Host) hem de tüm Client'larda çalýþacaktýr.
        PlaceLocalPlayer();
    }


    /// <summary>
    /// Her client, kendi yerel oyuncusunu doðru spawn noktasýna yerleþtirir.
    /// Bu metod, tüm client'larda çaðrýlmalýdýr (Host da dahil).
    /// </summary>
    public void PlaceLocalPlayer()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayer == null)
        {
            Debug.LogError("[DevSingletonTransform] Local Player Object bulunamadý!");
            return;
        }

        var playerSC = localPlayer.GetComponent<PlayerSC>();

        if (playerSC != null)
        {
            // NetworkVariable'ýn deðerinin senkronize olmasýný bekle.
            if (playerSC.TeamId.Value == 1)
            {
                // Local Player'ýn Transform'unu deðiþtirmek için yetkimiz var.
                localPlayer.transform.SetPositionAndRotation(player1Transform.position, player1Transform.rotation);
                Debug.Log($"[Client-{NetworkManager.Singleton.LocalClientId}] Team 1 (Player 1) noktasýna yerleþtirildi.");
            }
            else if (playerSC.TeamId.Value == 2)
            {
                localPlayer.transform.SetPositionAndRotation(player2Transform.position, player2Transform.rotation);
                Debug.Log($"[Client-{NetworkManager.Singleton.LocalClientId}] Team 2 (Player 2) noktasýna yerleþtirildi.");
            }
        }
        else
        {
            Debug.LogError("[DevSingletonTransform] Local PlayerSC component'i bulunamadý!");
        }
    }
}
