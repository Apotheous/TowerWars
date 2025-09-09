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
        // Eðer bu obje local player deðilse kamerayý deaktif et
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false; // Kamerayý deaktif et
                Debug.Log("Uzak oyuncunun kamerasý deaktif edildi");
            }
        }
        else
        {
            // Local player'ýn kamerasý aktif olsun
            if (playerCamera != null)
            {
                playerCamera.enabled = true; // Kamerayý aktif et
                Debug.Log("Local oyuncunun kamerasý aktif edildi");
            }
        }
    }
}
