using Unity.Netcode;
using UnityEngine;

public class PlayerCamOpener : NetworkBehaviour
{
    [SerializeField] private GameObject playerCamera;

    void Start()
    {
        // Eðer bu obje local player deðilse kamerayý deaktif et
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.SetActive(false);
            }
        }
        else
        {
            // Local player'ýn kamerasý aktif olsun
            if (playerCamera != null)
            {
                playerCamera.SetActive(false);
            }
        }
    }
}
