using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class OneVsOneGameManager : NetworkBehaviour
{
    public static OneVsOneGameManager Instance { get; private set; }

    public NetworkVariable<bool> GameOver = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> WinnerClientId = new NetworkVariable<ulong>(0);


    [SerializeField] private GameObject gameSceneDevCam;

    public static event Action ActOnGameSceneStarted;
    public static event Action ActOnGameSceneClosed;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {

        if (gameSceneDevCam != null && NetworkManager.Singleton != null)
        {
            gameSceneDevCam.SetActive(false);
        }
        // Local player objesini al
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (player != null)
        {
            var modeChanger = player.GetComponent<Player_Game_Mode_Manager>();
            if (modeChanger != null)
            {
                // Event örneði (mode deðiþtiðinde tetiklenecek)
                modeChanger.OnModeChanged += ModeChangedHandler;

                // Sadece sahibi serverRPC ile deðiþiklik yapabilir
                if (modeChanger.IsOwner)
                {
                    modeChanger.RequestStartGameServerRpc();
                }
            }
        }
    }

    public void OnGameSceneOpening()
    {
        Debug.Log("MainSceneManager Start: Main scene initialized");

        ActOnGameSceneStarted?.Invoke();
    }
    public void OnGameSceneClosed()
    {
        Debug.Log("MainSceneManager Start: Main scene initialized");

        ActOnGameSceneClosed?.Invoke();
    }

    private void ModeChangedHandler(Player_Game_Mode_Manager.PlayerMode newMode)
    {
        Debug.Log("GameSceneMainManager detected new mode: " + newMode);
        // Buraya ek oyun mantýklarý ekleyebilirsin
    }

    // Çaðrýlacak: server-side olduðunda PlayerSC.Die() içinde direkt çaðrýlabilir
    public void HandlePlayerDeath(ulong deadClientId)
    {
        if (!IsServer) return;
        if (GameOver.Value) return; // zaten bitti ise ignore

        // diðer oyuncuyu bul
        var other = NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault(c => c.ClientId != deadClientId);

        if (other == null)
        {
            // rakip yok -> tie / disconnect
            GameOver.Value = true;
            WinnerClientId.Value = 0; // 0 => no winner / tie
            NotifyGameOverClientRpc(0, true);
            return;
        }

        GameOver.Value = true;
        WinnerClientId.Value = other.ClientId;
        NotifyGameOverClientRpc(other.ClientId, false);
    }

    [ClientRpc]
    private void NotifyGameOverClientRpc(ulong winnerClientId, bool isTie)
    {
        // client-side: UI ve input yönetimi
        ulong selfId = NetworkManager.Singleton.LocalClientId;
        bool iWon = (!isTie) && (winnerClientId == selfId);

        string msg = isTie ? "Berabere!" : (iWon ? "Kazandýn!" : "Kaybettin!");
        // UI singletona bildir
        OneVsOneGameSceneUISingleton.Instance.ShowGameOver(msg, iWon);
    }

    // opsiyonel: restart veya return-to-lobby ServerRpc, ClientRpc vb.


}
