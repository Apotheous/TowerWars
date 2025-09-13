using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TestBullet : NetworkBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime); // belli süre sonra yok olsun
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Sadece server tarafýnda hasar uygula

        PlayerSC player = other.GetComponent<PlayerSC>();
        if (player != null)
        {
            player.UpdateMyCurrentHealth(-damage); // Hasar uygula
            Destroy(gameObject); // çarpýnca yok et
        }
    }
}
