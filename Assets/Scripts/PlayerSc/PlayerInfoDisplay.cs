using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerInfoDisplay : NetworkBehaviour
{
    // Inspector'dan, oyuncu prefab'ýnýn altýndaki TextMeshPro objesini buraya sürükleyin.
    [SerializeField]
    private TextMeshProUGUI playerInfoText;

    /// <summary>
    // Bu metot, obje network'te spawn olduðunda hem server'da hem de tüm client'larda bir kez çalýþýr.
    // Oyuncu kimliði gibi network bilgilerine eriþmek için en doðru yer burasýdýr.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Referansýn atanýp atanmadýðýný kontrol et, hata almamak için önemlidir.
        if (playerInfoText == null)
        {
            Debug.LogError("PlayerInfoText referansý bu objeye atanmamýþ: " + gameObject.name);
            return;
        }

        // OwnerClientId, bu objenin sahibi olan client'ýn kimliðidir.
        // Host/Server için 0, ilk baðlanan client için 1, ikincisi için 2 diye gider.
        // Kullanýcýya daha anlaþýlýr göstermek için 1 ekliyoruz.
        ulong playerNumber = OwnerClientId ;

        // TextMeshPro bileþeninin metnini güncelle.
        playerInfoText.text = "Player " + playerNumber;
        if (CloudSaveAccountManagerGameScene.Instance!=null)
        {
            var cloudName = CloudSaveAccountManagerMainScene.Instance.GetMyPlayerName();
            playerInfoText.text = "Player_Number_" + playerNumber+"_AccountName_"+ cloudName; 
        }
    }
}
