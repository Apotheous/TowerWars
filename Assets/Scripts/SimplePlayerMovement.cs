using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SimplePlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        if (true)
        {

        }
    }


    void Update()
    {
        // Sadece local player kendi inputunu kontrol etsin
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal"); // A, D veya Sol/Sa� ok
        float v = Input.GetAxis("Vertical");   // W, S veya Yukar�/A�a�� ok

        Vector3 move = new Vector3(h, 0f, v) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
}
