using System;
using Unity.Netcode;
using UnityEngine;

public class GameSceneMainManager : NetworkBehaviour
{
    [SerializeField] private GameObject gameSceneDevCam;

    public static event Action ActOnGameSceneStarted;
    public static event Action ActOnGameSceneClosed;


    private void Start()
    {

        if (gameSceneDevCam != null && NetworkManager.Singleton!=null)
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


}
