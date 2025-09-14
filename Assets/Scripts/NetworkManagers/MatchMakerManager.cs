using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MatchMakerManager : NetworkBehaviour
{
    //[SerializeField] private TMP_Dropdown gameModeDropdown;
    public static MatchMakerManager Instance;

    private PayloadAllocation payloadAllocation;
    private IMatchmakerService matchmakerService;
    private string backfillTicketId;

    private NetworkManager networkManager;
    private string currentTicket;

    [SerializeField] private MatchMakerUI matchMakerUI;

    [SerializeField] string sceneName;

    private void Awake()
    {
        // Eðer zaten bir Instance varsa ve bu o deðilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Sahne deðiþince kaybolmasýn
    }
    private async void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        }
        else
        {
            while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Yield();
            }

            matchmakerService = MatchmakerService.Instance;
            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
            backfillTicketId = payloadAllocation.BackfillTicketId;
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;

    }
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"Player connected! ClientId: {clientId}");
        CheckConnectedTwoPlayers();

    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player disconnected! ClientId: {clientId}");
    }

    private void CheckConnectedTwoPlayers()
    {
        if (networkManager.ConnectedClientsList.Count==2)
        {

            LoadGameScene(sceneName);
        }
    }

    public void LoadGameScene(string sceneName)
    {
        // Sadece server/host sahne deðiþikliði yapabilir
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only server/host can change scenes!");
            return;
        }

        // Sahne geçiþi öncesi hazýrlýk
        PrepareSceneTransition();

        Debug.Log($"Loading scene: {sceneName}");

        // NetworkManager'ýn SceneManager'ýný kullan
        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);


        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start loading scene {sceneName}");
        }


    }

    private void PrepareSceneTransition()
    {
        // Sahne geçiþi öncesi temizlik iþlemleri
        // Örneðin: UI panellerini kapat, geçici objeleri temizle
    }

    bool isDeallocating = false;
    bool deallocatingCancellationToken = false;

    private async void Update()
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 0 && !isDeallocating)
            {
                isDeallocating = true;
                deallocatingCancellationToken = false;
                Deallocate();
            }

            if (NetworkManager.Singleton.ConnectedClientsList.Count != 0)
            {
                isDeallocating = false;
                deallocatingCancellationToken = true;
            }

            if (backfillTicketId != null && NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            {
                BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                backfillTicketId = backfillTicket.Id;
                
            }


            await Task.Delay(1000);
        }


    }

    private void OnPlayerConnected()
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }
    private void OnPlayerDisconnected()
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }


    private async void UpdateBackfillTicket()
    {
        List<Player> players = new List<Player>();

        foreach (ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            players.Add(new Player(playerId.ToString()));
        }

        MatchProperties matchProperties = new MatchProperties(null, players, null, backfillTicketId);

        await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId,
            new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties)));
    }


    private async void Deallocate()
    {
        await Task.Delay(60 * 1000);

        if (!deallocatingCancellationToken)
        {
            Application.Quit();
        }
    }




    public async void ClientJoin()
    {
        //await CreateAndStoreTicketAsync();
        //await PollTicketStatusAsync(currentTicket);
        CreateTicketOptions createTicketOptions = new CreateTicketOptions("MyQueue",
           new Dictionary<string, object> { { "GameMode", "EasyMode" } });//gameModeDropdown.options[gameModeDropdown.value].text

        List<Player> players = new List<Player> { new Player(AuthenticationService.Instance.PlayerId) };

        CreateTicketResponse createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
        currentTicket = createTicketResponse.Id;
        Debug.Log("Ticket created");

        while (true)
        {
            TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);

            if (ticketStatusResponse.Type == typeof(MultiplayAssignment))
            {
                MultiplayAssignment multiplayAssignment = (MultiplayAssignment)ticketStatusResponse.Value;

                if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Found)
                {
                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetConnectionData(multiplayAssignment.Ip, ushort.Parse(multiplayAssignment.Port.ToString()));
                    NetworkManager.Singleton.StartClient();

                    Debug.Log("Match found");

                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Timeout)
                {
                    Debug.Log("Match timeout");
                    ClientJoin();
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                {
                    Debug.Log("Match failed" + multiplayAssignment.Status + "  " + multiplayAssignment.Message);
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.InProgress)
                {
                    Debug.Log("Match is in progress");

                }

            }

            await Task.Delay(1000);
        }
    }

    private async Task CreateAndStoreTicketAsync()
    {
        // Matchmaker queue için ticket oluþturma seçeneklerini ayarla
        // "MyQueue" adlý queue'ya katýlmak için gerekli parametreleri belirle
        CreateTicketOptions createTicketOptions = new CreateTicketOptions("MyQueue",
            new Dictionary<string, object> { { "GameMode", "EasyMode" } });

        // Bu ticket için oyuncu listesini oluþtur
        // Þu anda sadece bu client'ýn oyuncusu (AuthenticationService'den alýnan PlayerId) eklenir
        List<Player> players = new List<Player>
        {
            new Player(AuthenticationService.Instance.PlayerId)
        };
        // Matchmaker servisine ticket oluþturma isteði gönder
        // Bu asenkron iþlem tamamlandýðýnda CreateTicketResponse döner
        CreateTicketResponse createTicketResponse =
            await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);

        // Dönen response'dan ticket ID'yi al ve currentTicket deðiþkeninde sakla
        // Bu ID daha sonra ticket durumunu kontrol etmek için kullanýlacak
        currentTicket = createTicketResponse.Id;

        Debug.Log($"Ticket created: {currentTicket}");
        DebugManager.Instance?.Log3(currentTicket.ToString());
    }

    private async Task PollTicketStatusAsync(string ticketId)
    {
        // Sonsuz döngü - ticket durumu sürekli kontrol edilir
        while (true)
        {
            // Matchmaker servisinden verilen ticket ID'nin durumunu asenkron olarak sorgula
            // Bu iþlem ticket'ýn hangi aþamada olduðunu (beklemede, atanmýþ, vs.) döner
            TicketStatusResponse ticketStatusResponse =
                await MatchmakerService.Instance.GetTicketAsync(ticketId);
            // Eðer ticket durumu MultiplayAssignment tipindeyse (sunucu atanmýþ demektir)
            if (ticketStatusResponse.Type == typeof(MultiplayAssignment))
            {
                // Response'u MultiplayAssignment tipine cast et
                // Bu assignment sunucu IP, port ve diðer baðlantý bilgilerini içerir
                var assignment = (MultiplayAssignment)ticketStatusResponse.Value;

                // Assignment'ý iþle (sunucuya baðlanma iþlemi)
                // HandleAssignmentAsync metodu true/false döner (baþarýlý/baþarýsýz)
                bool handled = await HandleAssignmentAsync(assignment);

                // Eðer assignment baþarýyla iþlendiyse (baþarýlý veya baþarýsýz olsun)
                if (handled)
                    return; // Polling döngüsünden çýk - iþlem tamamlandý
            }
            // 1 saniye bekle ve tekrar ticket durumunu kontrol et
            // Bu sürekli spam yapmayý önler ve sunucuya aþýrý yük bindirmez
            await Task.Delay(1000);
        }
    }


    // Sýradan çýk 
    public async Task LeaveQueueAsync()
    {
        if (string.IsNullOrEmpty(currentTicket))
        {
            Debug.Log("Aktif ticket yok, sýradan çýkýlamaz.");
            return;
        }

        try
        {
            // Önce ticket'ý iptal et
            await MatchmakerService.Instance.DeleteTicketAsync(currentTicket);
            Debug.Log($"Ticket iptal edildi: {currentTicket}");
            currentTicket = null;

            // Network baðlantýsýný temiz þekilde kapat
            DisconnectFromNetwork();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LeaveQueue hatasý: {ex.Message}");
            // Hata olsa bile network'ü kapat
            DisconnectFromNetwork();
        }
    }

    private void DisconnectFromNetwork()
    {
        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            if (networkManager != null && networkManager.IsConnectedClient)
            {
                Debug.Log("Network baðlantýsý kapatýlýyor...");
                // Client olarak baðlantýyý kapat
                networkManager.Shutdown();
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Eðer Linux sunucusu deðilse (client/host ise bu kýsým çalýþýr)
        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            // Eðer bu client bir networke baðlýysa
            if (networkManager.IsConnectedClient)
            {
                // NetworkManager'ý kapat (true = zorla kapat)
                // Bu tüm network baðlantýlarýný temizler
                networkManager.Shutdown(true);
                
                // Bu client'ý network'ten kopar
                // OwnerClientId = bu script'in sahibi olan client'ýn ID'si
                networkManager.DisconnectClient(OwnerClientId);
            }
        }
    }
    private async Task<bool> HandleAssignmentAsync(MultiplayAssignment assignment)
    {
        switch (assignment.Status)
        {
            case MultiplayAssignment.StatusOptions.Found:
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetConnectionData(assignment.Ip, ushort.Parse(assignment.Port.ToString()));
                NetworkManager.Singleton.StartClient();

                Debug.Log("? Match found, connecting to server...");
                return true;

            case MultiplayAssignment.StatusOptions.Timeout:
                Debug.Log("?? Match timeout, retrying...");
                await CreateAndStoreTicketAsync();
                return false; // yeniden ticket oluþturulup polling devam edecek

            case MultiplayAssignment.StatusOptions.Failed:
                Debug.LogError($"? Match failed: {assignment.Message}");
                return true;

            case MultiplayAssignment.StatusOptions.InProgress:
                Debug.Log("? Match is still in progress...");
                return false;

            default:
                Debug.LogWarning("Unknown assignment status");
                return false;
        }
    }



    [System.Serializable]
    public class PayloadAllocation
    {
        public MatchProperties MatchProperties;
        public string GeneratorName;
        public string QueueName;
        public string PoolName;
        public string EnvironmentId;
        public string BackfillTicketId;
        public string MatchId;
        public string PoolId;
    }

}
