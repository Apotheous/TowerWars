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

    private PayloadAllocation payloadAllocation;
    private IMatchmakerService matchmakerService;
    private string backfillTicketId;

    private NetworkManager networkManager;
    private string currentTicket;


    [SerializeField] private TextMeshProUGUI onlinePlayerCountText;
    [SerializeField] private GameObject myPanel;


    [SerializeField] string sceneName;
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
        DebugManager.Instance.Log3(networkManager.ConnectedClientsIds.Count.ToString());
        CheckConnectedTwoPlayers();

    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player disconnected! ClientId: {clientId}");
    }

    private void CheckConnectedTwoPlayers()
    {
        if (networkManager.ConnectedClientsIds.Count==2)
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

    public void CloseMyPanel()
    {
        myPanel.SetActive(false);
    }
    public void OpenMyPanel()
    {
        myPanel.SetActive(true);
      
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

    private void OnApplicationQuit()
    {
        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            if (networkManager.IsConnectedClient)
            {
                networkManager.Shutdown(true);
                networkManager.DisconnectClient(OwnerClientId);
            }
        }
    }


    public async void ClientJoin()
    {
        await CreateAndStoreTicketAsync();
        await PollTicketStatusAsync(currentTicket);
    }

    private async Task CreateAndStoreTicketAsync()
    {
        CreateTicketOptions createTicketOptions = new CreateTicketOptions("MyQueue",
            new Dictionary<string, object> { { "GameMode", "EasyMode" } });

        List<Player> players = new List<Player>
        {
            new Player(AuthenticationService.Instance.PlayerId)
        };

        CreateTicketResponse createTicketResponse =
            await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);

        currentTicket = createTicketResponse.Id;

        Debug.Log($"Ticket created: {currentTicket}");
    }

    private async Task PollTicketStatusAsync(string ticketId)
    {
        while (true)
        {
            TicketStatusResponse ticketStatusResponse =
                await MatchmakerService.Instance.GetTicketAsync(ticketId);

            if (ticketStatusResponse.Type == typeof(MultiplayAssignment))
            {
                var assignment = (MultiplayAssignment)ticketStatusResponse.Value;
                bool handled = await HandleAssignmentAsync(assignment);

                if (handled)
                    return; // baþarýlý ya da baþarýsýz ? döngüden çýk
            }

            await Task.Delay(1000);
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
