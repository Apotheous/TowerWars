using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamController : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;

    void Start()
    {
        // E�er bu obje local player de�ilse kameray� deaktif et
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false; // Kameray� deaktif et
                Debug.Log("Uzak oyuncunun kameras� deaktif edildi");
            }
        }
        else
        {
            // Local player'�n kameras� aktif olsun
            if (playerCamera != null)
            {
                playerCamera.enabled = true; // Kameray� aktif et
                Debug.Log("Local oyuncunun kameras� aktif edildi");
            }
        }
    }
}
