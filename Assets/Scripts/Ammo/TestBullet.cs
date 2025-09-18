using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class TestBullet : NetworkBehaviour
{
    public float speed = 10f;
    public float myDamage = 10f;
    public float lifetime = 3f;

    private void Start()
    {
        if (IsServer)
        {
            StartCoroutine(DelayedDespawn());
        }
    }

    private IEnumerator DelayedDespawn()
    {
        yield return new WaitForSeconds(lifetime);
        DespawnBulletServerRpc(NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnBulletServerRpc(ulong bulletId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bulletId, out var bulletObj))
        {
            bulletObj.Despawn(); // Pooling varsa bunu kullan, yoksa Destroy(bulletObj.gameObject)
        }
    }


    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider c)
    {
        //if (!IsServer) return; // Sadece server tarafýnda hasar uygula
        var dmg = c.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(25f);
        }
        else
        {
            myDamage = 0;
        }
        

        //if (c != null)
        //{
        //    player.UpdateMyCurrentHealth(-myDamage); // Hasar uygula
        //    player.UpdateMyScrap(myDamage);
        //    player.UpdateExpPointIncrease(myDamage);

        //    Destroy(gameObject); // çarpýnca yok et
        //}
    }



}
