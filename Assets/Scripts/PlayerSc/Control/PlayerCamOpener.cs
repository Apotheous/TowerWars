using Unity.Netcode;
using UnityEngine;

public class PlayerCamOpener : NetworkBehaviour
{
    [SerializeField] private GameObject playerCamera;

    void Start()
    {
        // E�er bu obje local player de�ilse kameray� deaktif et
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.SetActive(false);
            }
        }
        else
        {
            // Local player'�n kameras� aktif olsun
            if (playerCamera != null)
            {
                playerCamera.SetActive(false);
            }
        }
    }
}
