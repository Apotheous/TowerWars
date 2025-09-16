using Unity.Netcode;
using UnityEngine;

public class GameSceneMainManager : NetworkBehaviour
{
    private void Start()
    {
        // Local player objesini al
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (player != null)
        {
            var modeChanger = player.GetComponent<Player_Game_Mode_Manager>();
            if (modeChanger != null)
            {
                // Event �rne�i (mode de�i�ti�inde tetiklenecek)
                modeChanger.OnModeChanged += ModeChangedHandler;

                // Sadece sahibi serverRPC ile de�i�iklik yapabilir
                if (modeChanger.IsOwner)
                {
                    modeChanger.RequestStartGameServerRpc();
                }
            }
        }
    }

    private void ModeChangedHandler(Player_Game_Mode_Manager.PlayerMode newMode)
    {
        Debug.Log("GameSceneMainManager detected new mode: " + newMode);
        // Buraya ek oyun mant�klar� ekleyebilirsin
    }


}
