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

    // YEN�: Hizmetlerin ba�lat�l�p ba�lat�lmad���n� kontrol eden bayrak
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
            // �stemci/Host Taraf� Ba�latma
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            ServicesInitialized = true; // Bayra�� ayarla
        }
        else
        {
            // Linux Sunucu Taraf� Ba�latma

            // Ba�latma tamamlanana kadar bekle (Bu blok orijinal kodunuzdan geldi, korunmu�tur)
            while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Yield();
            }

            matchmakerService = MatchmakerService.Instance;

            // Multiplay payload'unu al
            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
            backfillTicketId = payloadAllocation.BackfillTicketId;

            // HATA ��Z�M� #2: Kontroll� Backfill d�ng�s�n� Start() i�inde ba�lat
            StartBackfillPolling();
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;
    }

    // YEN� VE D�ZELT�LM��: 429 hatalar�n� �nlemek i�in �zel asenkron d�ng�
    private async void StartBackfillPolling()
    {
        // Sonsuz d�ng�: Sunucunun �mr� boyunca �al��acak
        while (Application.platform == RuntimePlatform.LinuxServer)
        {
            try
            {
                // Backfill mant���: backfillTicketId varsa ve hala yer varsa (2'den az oyuncu)
                if (backfillTicketId != null && NetworkManager.Singleton.ConnectedClientsList.Count < 2)
                {
                    Debug.Log($"[Matchmaker Polling] Backfill iste�i g�nderiliyor: {backfillTicketId}");

                    // ApproveBackfillTicketAsync'i kontroll� olarak �a��r
                    BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                    backfillTicketId = backfillTicket.Id;
                    Debug.Log($"[Matchmaker Polling] Backfill onay� ba�ar�l�. Yeni ID: {backfillTicketId}");
                }
            }
            catch (MatchmakerServiceException ex)
            {
                // D�ZELTME: Hata mesaj� i�inde "429" veya "Too Many Requests" kontrol�
                if (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
                {
                    Debug.LogError($"[Matchmaker Polling ERROR] HTTP 429 Uyar�s�: Matchmaker bizi engelliyor. Bir sonraki deneme i�in 15 saniye beklenecek.");
                    // API taraf�ndan engellendi�imiz i�in 1 saniye yerine 15 saniye bekleyin (Backoff)
                    await Task.Delay(15000);
                    continue; // Bekledikten sonra d�ng�y� hemen ba�lat
                }
                // Log kayd�n�zda g�r�len 'Backfill Ticket not found' hatas�n� ele al�n.
                else if (ex.Reason == MatchmakerExceptionReason.EntityNotFound)
                {
                    Debug.LogWarning($"[Matchmaker Polling WARNING] Backfill Ticket bulunamad�. ID: {backfillTicketId}");
                    // Backfill ticket ID'sini null yap, s�rekli bulunamayan bileti sorgulamas�n
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

            // Ba�ar�l� veya di�er k���k hatalarda (429 hari�) 5 saniye bekleme.
            await Task.Delay(5000);
        }
    }

    // Orijinal Event Ba�lama/Ay�rma Metotlar�
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
        // �stemci ba�land���nda backfill biletini g�ncelle (eksik mant�k tamamlanm��t�r)
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player disconnected! ClientId: {clientId}");
        // �stemci ayr�ld���nda backfill biletini g�ncelle (eksik mant�k tamamlanm��t�r)
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
        // Sahne ge�i�i �ncesi temizlik i�lemleri
    }

    bool isDeallocating = false;
    bool deallocatingCancellationToken = false;

    // HATA ��Z�M� #1: Update metodunu async/await ve Task.Delay'den kurtar�lm�� hali
    private void Update()
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            // Yaln�zca Deallocate (Ay�rma) mant���n� b�rak
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 0 && !isDeallocating)
            {
                isDeallocating = true;
                deallocatingCancellationToken = false;
                Deallocate();
            }

            if (NetworkManager.Singleton.ConnectedClientsList.Count != 0)
            {
                // Sunucuya oyuncu ba�land�, Deallocate i�lemini iptal et.
                isDeallocating = false;
                deallocatingCancellationToken = true;
            }
        }
    }

    // Orijinal metodlar temizlendi. OnPlayerConnected/Disconnected i�indeki mant�k HandleClientConnected/Disconnected'a ta��nd�.
    // private void OnPlayerConnected() ve private void OnPlayerDisconnected() metotlar� silinmi�tir, ��nk� ayn� i�i yapan HandleClientConnected/Disconnected metotlar�n�z zaten mevcuttur.

    private async void UpdateBackfillTicket()
    {
        // Orijinal Match Properties ve Oyuncu Listesi (UGS Player ID'leri)
        // Bu, Matchmaker'dan gelen Teams ve Players listesini i�erir.
        MatchProperties originalMatchProperties = payloadAllocation.MatchProperties;
        // Orijinal UGS Player ID'leri listesi (UGS'nin bekledi�i Player objeleri)
        List<Player> ugsPlayers = originalMatchProperties.Players;

        // Yeni ba�l� oyuncu listesini olu�tur.
        // Sadece ba�l� olan oyuncular�n UGS Player ID'lerini (Matchmaker'�n bekledi�i) al.
        List<Player> currentPlayers = new List<Player>();

        // Hata 1'i ��zer: IReadOnlyList'ten ka��nmak i�in ba�l� client say�s�n� kontrol et.
        int connectedCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Ba�l� olan her bir client i�in, orijinal Matchmaker listesindeki s�ras�na g�re UGS ID'sini kullan.
        // Bu, Matchmaker'�n Player listesi ile Network'e ba�lanan Player'lar�n e�le�ti�ini varsayar.
        for (int i = 0; i < connectedCount && i < ugsPlayers.Count; i++)
        {
            currentPlayers.Add(ugsPlayers[i]);
        }

        // Hata 2'yi ��zer: Yeni MatchProperties nesnesini sadece Teams ve Players ile olu�tur.
        // AllocatedPlayers ve BackfillTicketId gibi ek parametreler kald�r�ld�.
        MatchProperties updatedMatchProperties = new MatchProperties(
            teams: originalMatchProperties.Teams,                // Orijinal tak�m verisini koru (KR�T�K)
            players: currentPlayers                              // G�ncel ba�l� oyuncu listesini g�nder
        );

        // G�ncellenmi� bilet nesnesini olu�tur
        // BackfillTicketProperties, g�ncellenmi� MatchProperties nesnesini al�r.
        BackfillTicket updatedTicket = new BackfillTicket(
            backfillTicketId,
            properties: new BackfillTicketProperties(updatedMatchProperties)
        );

        // G�ncellenmi� bileti Matchmaker'a g�nderin.
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


    // YEN� VE D�ZELT�LM��: Hizmet ba�latma kontrol� eklendi.
    public async void ClientJoin()
    {
        // YEN� D�ZELTME: AuthenticationService kullanmadan �nce hizmetlerin haz�r oldu�unu kontrol et.
        if (!ServicesInitialized)
        {
            Debug.LogError("Unity Services hen�z ba�lat�lmad�. L�tfen ba�latma i�leminin tamamlanmas�n� bekleyin.");
            // E�er butonu devre d��� b�rakma se�ene�iniz yoksa, burada ek bir bekleme mant��� eklenebilir.
            return;
        }

        CreateTicketOptions createTicketOptions = new CreateTicketOptions("MyQueue",
           new Dictionary<string, object> { { "GameMode", "EasyMode" } });

        // Bu noktada AuthenticationService'in kullan�lmaya haz�r oldu�u garanti edilir.
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
                    // �zyinelemeli (recursive) �a�r� yerine d�ng�den ��k�p yeni ticket olu�turmak i�in bir flag kullanmak daha g�venlidir, 
                    // ancak orijinal mant���n�z� koruyarak ClientJoin() �a�r�s�n� b�rakt�m.
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

            await Task.Delay(1000); // Polling (S�rekli kontrol) bekleme s�resi
        }
    }

    // Bu metotlar� ClientJoin'in sadele�tirilmi� hali oldu�u i�in korudum
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
        // DebugManager.Instance?.Log3(currentTicket.ToString()); // Harici ba��ml�l�klar� temizledim
    }

    // Bu metot ClientJoin() i�inde yeniden yaz�ld��� i�in orijinal metot (PollTicketStatusAsync) korunmu�tur, ancak ClientJoin() onu kullanmaz.
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
            Debug.Log("Aktif ticket yok, s�radan ��k�lamaz.");
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
            Debug.LogError($"LeaveQueue hatas�: {ex.Message}");
            DisconnectFromNetwork();
        }
    }

    private void DisconnectFromNetwork()
    {
        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            if (networkManager != null && networkManager.IsConnectedClient)
            {
                Debug.Log("Network ba�lant�s� kapat�l�yor...");
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
                // Dikkat: OwnerClientId, NetworkBehaviour s�n�f�ndan gelir, ancak NetworkManager'dan ayr�l�rken her zaman ge�erli olmayabilir.
                // E�er bu client'�n kendi ID'si ise sorun yok.
                // networkManager.DisconnectClient(OwnerClientId); // Bu sat�r� kald�rd�m, Shutdown(true) genellikle yeterlidir.
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
