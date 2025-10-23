using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerInfoDisplay : NetworkBehaviour
{
    // Inspector'dan, oyuncu prefab'�n�n alt�ndaki TextMeshPro objesini buraya s�r�kleyin.
    [SerializeField]
    private TextMeshProUGUI playerInfoText;

    /// <summary>
    // Bu metot, obje network'te spawn oldu�unda hem server'da hem de t�m client'larda bir kez �al���r.
    // Oyuncu kimli�i gibi network bilgilerine eri�mek i�in en do�ru yer buras�d�r.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Referans�n atan�p atanmad���n� kontrol et, hata almamak i�in �nemlidir.
        if (playerInfoText == null)
        {
            Debug.LogError("PlayerInfoText referans� bu objeye atanmam��: " + gameObject.name);
            return;
        }

        // OwnerClientId, bu objenin sahibi olan client'�n kimli�idir.
        // Host/Server i�in 0, ilk ba�lanan client i�in 1, ikincisi i�in 2 diye gider.
        // Kullan�c�ya daha anla��l�r g�stermek i�in 1 ekliyoruz.
        ulong playerNumber = OwnerClientId ;

        // TextMeshPro bile�eninin metnini g�ncelle.
        playerInfoText.text = "Player " + playerNumber;
        if (CloudSaveAccountManagerGameScene.Instance!=null)
        {
            var cloudName = CloudSaveAccountManagerMainScene.Instance.GetMyPlayerName();
            playerInfoText.text = "Player_Number_" + playerNumber+"_AccountName_"+ cloudName; 
        }
    }
}
