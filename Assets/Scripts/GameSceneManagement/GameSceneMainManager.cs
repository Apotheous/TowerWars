using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static PlayerScene_and_Game_Mode_Changer;

public class GameSceneMainManager : MonoBehaviour
{
    private void Start()
    {
        // Local player objesini al
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (player != null)
        {
            var modeChanger = player.GetComponent<PlayerScene_and_Game_Mode_Changer>();
            if (modeChanger != null)
            {
                // Event örneði (mode deðiþtiðinde tetiklenecek)
                modeChanger.OnModeChanged += ModeChangedHandler;

                // Sadece sahibi serverRPC ile deðiþiklik yapabilir
                if (modeChanger.IsOwner)
                {
                    modeChanger.SetModeServerRpc(PlayerScene_and_Game_Mode_Changer.PlayerMode.OneVsOne);
                }
            }
        }
    }

    private void ModeChangedHandler(PlayerScene_and_Game_Mode_Changer.PlayerMode newMode)
    {
        Debug.Log("GameSceneMainManager detected new mode: " + newMode);
        // Buraya ek oyun mantýklarý ekleyebilirsin
    }


}
