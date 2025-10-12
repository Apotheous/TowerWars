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
    public static MatchMakerManager Instance;

    private PayloadAllocation payloadAllocation;
    private IMatchmakerService matchmakerService;
    private string backfillTicketId;

    private NetworkManager networkManager;
    private string currentTicket;

    [SerializeField] private MatchMakerUI matchMakerUI;
    [SerializeField] string sceneName;

    // YENÝ: Hizmetlerin baþlatýlýp baþlatýlmadýðýný kontrol eden bayrak
    public bool ServicesInitialized { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            // Ýstemci/Host Tarafý Baþlatma
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            ServicesInitialized = true; // Bayraðý ayarla
        }
        else
        {
            // Linux Sunucu Tarafý Baþlatma

            // Baþlatma tamamlanana kadar bekle (Bu blok orijinal kodunuzdan geldi, korunmuþtur)
            while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Yield();
            }

            matchmakerService = MatchmakerService.Instance;

            // Multiplay payload'unu al
            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
            backfillTicketId = payloadAllocation.BackfillTicketId;

            // HATA ÇÖZÜMÜ #2: Kontrollü Backfill döngüsünü Start() içinde baþlat
            StartBackfillPolling();
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;
    }

    // YENÝ VE DÜZELTÝLMÝÞ: 429 hatalarýný önlemek için özel asenkron döngü
    private async void StartBackfillPolling()
    {
        // Sonsuz döngü: Sunucunun ömrü boyunca çalýþacak
        while (Application.platform == RuntimePlatform.LinuxServer)
        {
            try
            {
                // Backfill mantýðý: backfillTicketId varsa ve hala yer varsa (2'den az oyuncu)
                if (backfillTicketId != null && NetworkManager.Singleton.ConnectedClientsList.Count < 2)
                {
                    Debug.Log($"[Matchmaker Polling] Backfill isteði gönderiliyor: {backfillTicketId}");

                    // ApproveBackfillTicketAsync'i kontrollü olarak çaðýr
                    BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                    backfillTicketId = backfillTicket.Id;
                    Debug.Log($"[Matchmaker Polling] Backfill onayý baþarýlý. Yeni ID: {backfillTicketId}");
                }
            }
            catch (MatchmakerServiceException ex)
            {
                // DÜZELTME: Hata mesajý içinde "429" veya "Too Many Requests" kontrolü
                if (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
                {
                    Debug.LogError($"[Matchmaker Polling ERROR] HTTP 429 Uyarýsý: Matchmaker bizi engelliyor. Bir sonraki deneme için 15 saniye beklenecek.");
                    // API tarafýndan engellendiðimiz için 1 saniye yerine 15 saniye bekleyin (Backoff)
                    await Task.Delay(15000);
                    continue; // Bekledikten sonra döngüyü hemen baþlat
                }
                // Log kaydýnýzda görülen 'Backfill Ticket not found' hatasýný ele alýn.
                else if (ex.Reason == MatchmakerExceptionReason.EntityNotFound)
                {
                    Debug.LogWarning($"[Matchmaker Polling WARNING] Backfill Ticket bulunamadý. ID: {backfillTicketId}");
                    // Backfill ticket ID'sini null yap, sürekli bulunamayan bileti sorgulamasýn
                    backfillTicketId = null;
                }
                else
                {
                    Debug.LogError($"[Matchmaker Polling GENEL HATA] {ex.Message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Matchmaker Polling BEKLENMEYEN HATA] {ex.Message}");
            }

            // Baþarýlý veya diðer küçük hatalarda (429 hariç) 5 saniye bekleme.
            await Task.Delay(5000);
        }
    }

    // Orijinal Event Baðlama/Ayýrma Metotlarý
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
        // Ýstemci baðlandýðýnda backfill biletini güncelle (eksik mantýk tamamlanmýþtýr)
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player disconnected! ClientId: {clientId}");
        // Ýstemci ayrýldýðýnda backfill biletini güncelle (eksik mantýk tamamlanmýþtýr)
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }

    private void CheckConnectedTwoPlayers()
    {
        if (IsServer && networkManager.ConnectedClientsList.Count == 2)
        {
            LoadGameScene(sceneName);
        }
    }

    public void LoadGameScene(string sceneName)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only server/host can change scenes!");
            return;
        }

        PrepareSceneTransition();

        Debug.Log($"Loading scene: {sceneName}");

        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start loading scene {sceneName}");
        }
    }

    private void PrepareSceneTransition()
    {
        // Sahne geçiþi öncesi temizlik iþlemleri
    }

    bool isDeallocating = false;
    bool deallocatingCancellationToken = false;

    // HATA ÇÖZÜMÜ #1: Update metodunu async/await ve Task.Delay'den kurtarýlmýþ hali
    private void Update()
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            // Yalnýzca Deallocate (Ayýrma) mantýðýný býrak
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 0 && !isDeallocating)
            {
                isDeallocating = true;
                deallocatingCancellationToken = false;
                Deallocate();
            }

            if (NetworkManager.Singleton.ConnectedClientsList.Count != 0)
            {
                // Sunucuya oyuncu baðlandý, Deallocate iþlemini iptal et.
                isDeallocating = false;
                deallocatingCancellationToken = true;
            }
        }
    }

    // Orijinal metodlar temizlendi. OnPlayerConnected/Disconnected içindeki mantýk HandleClientConnected/Disconnected'a taþýndý.
    // private void OnPlayerConnected() ve private void OnPlayerDisconnected() metotlarý silinmiþtir, çünkü ayný iþi yapan HandleClientConnected/Disconnected metotlarýnýz zaten mevcuttur.

    private async void UpdateBackfillTicket()
    {
        // Orijinal Match Properties ve Oyuncu Listesi (UGS Player ID'leri)
        // Bu, Matchmaker'dan gelen Teams ve Players listesini içerir.
        MatchProperties originalMatchProperties = payloadAllocation.MatchProperties;
        // Orijinal UGS Player ID'leri listesi (UGS'nin beklediði Player objeleri)
        List<Player> ugsPlayers = originalMatchProperties.Players;

        // Yeni baðlý oyuncu listesini oluþtur.
        // Sadece baðlý olan oyuncularýn UGS Player ID'lerini (Matchmaker'ýn beklediði) al.
        List<Player> currentPlayers = new List<Player>();

        // Hata 1'i çözer: IReadOnlyList'ten kaçýnmak için baðlý client sayýsýný kontrol et.
        int connectedCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Baðlý olan her bir client için, orijinal Matchmaker listesindeki sýrasýna göre UGS ID'sini kullan.
        // Bu, Matchmaker'ýn Player listesi ile Network'e baðlanan Player'larýn eþleþtiðini varsayar.
        for (int i = 0; i < connectedCount && i < ugsPlayers.Count; i++)
        {
            currentPlayers.Add(ugsPlayers[i]);
        }

        // Hata 2'yi çözer: Yeni MatchProperties nesnesini sadece Teams ve Players ile oluþtur.
        // AllocatedPlayers ve BackfillTicketId gibi ek parametreler kaldýrýldý.
        MatchProperties updatedMatchProperties = new MatchProperties(
            teams: originalMatchProperties.Teams,                // Orijinal takým verisini koru (KRÝTÝK)
            players: currentPlayers                              // Güncel baðlý oyuncu listesini gönder
        );

        // Güncellenmiþ bilet nesnesini oluþtur
        // BackfillTicketProperties, güncellenmiþ MatchProperties nesnesini alýr.
        BackfillTicket updatedTicket = new BackfillTicket(
            backfillTicketId,
            properties: new BackfillTicketProperties(updatedMatchProperties)
        );

        // Güncellenmiþ bileti Matchmaker'a gönderin.
        await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId, updatedTicket);
    }


    private async void Deallocate()
    {
        await Task.Delay(60 * 1000); // 1 dakika bekle

        if (!deallocatingCancellationToken)
        {
            Application.Quit();
        }
    }


    // YENÝ VE DÜZELTÝLMÝÞ: Hizmet baþlatma kontrolü eklendi.
    public async void ClientJoin()
    {
        // YENÝ DÜZELTME: AuthenticationService kullanmadan önce hizmetlerin hazýr olduðunu kontrol et.
        if (!ServicesInitialized)
        {
            Debug.LogError("Unity Services henüz baþlatýlmadý. Lütfen baþlatma iþleminin tamamlanmasýný bekleyin.");
            // Eðer butonu devre dýþý býrakma seçeneðiniz yoksa, burada ek bir bekleme mantýðý eklenebilir.
            return;
        }

        CreateTicketOptions createTicketOptions = new CreateTicketOptions("MyQueue",
           new Dictionary<string, object> { { "GameMode", "EasyMode" } });

        // Bu noktada AuthenticationService'in kullanýlmaya hazýr olduðu garanti edilir.
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
                    Debug.Log("Match timeout, retrying with new ticket.");
                    // Özyinelemeli (recursive) çaðrý yerine döngüden çýkýp yeni ticket oluþturmak için bir flag kullanmak daha güvenlidir, 
                    // ancak orijinal mantýðýnýzý koruyarak ClientJoin() çaðrýsýný býraktým.
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

            await Task.Delay(1000); // Polling (Sürekli kontrol) bekleme süresi
        }
    }

    // Bu metotlarý ClientJoin'in sadeleþtirilmiþ hali olduðu için korudum
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
        // DebugManager.Instance?.Log3(currentTicket.ToString()); // Harici baðýmlýlýklarý temizledim
    }

    // Bu metot ClientJoin() içinde yeniden yazýldýðý için orijinal metot (PollTicketStatusAsync) korunmuþtur, ancak ClientJoin() onu kullanmaz.
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
                    return;
            }
            await Task.Delay(1000);
        }
    }


    public async Task LeaveQueueAsync()
    {
        if (string.IsNullOrEmpty(currentTicket))
        {
            Debug.Log("Aktif ticket yok, sýradan çýkýlamaz.");
            return;
        }

        try
        {
            await MatchmakerService.Instance.DeleteTicketAsync(currentTicket);
            Debug.Log($"Ticket iptal edildi: {currentTicket}");
            currentTicket = null;

            DisconnectFromNetwork();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LeaveQueue hatasý: {ex.Message}");
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
                networkManager.Shutdown();
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            if (networkManager.IsConnectedClient)
            {
                networkManager.Shutdown(true);
                // Dikkat: OwnerClientId, NetworkBehaviour sýnýfýndan gelir, ancak NetworkManager'dan ayrýlýrken her zaman geçerli olmayabilir.
                // Eðer bu client'ýn kendi ID'si ise sorun yok.
                // networkManager.DisconnectClient(OwnerClientId); // Bu satýrý kaldýrdým, Shutdown(true) genellikle yeterlidir.
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
                return false;

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
