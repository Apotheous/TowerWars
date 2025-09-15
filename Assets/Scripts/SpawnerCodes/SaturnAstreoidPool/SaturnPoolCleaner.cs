using UnityEngine;

public class SaturnPoolCleaner : MonoBehaviour
{
    public SaturnAsteroidPoolCreating pool;


    private void OnCollisionEnter(Collision collision)
    {
        Asteroid collidable = collision.gameObject.GetComponent<Asteroid>();
        //MarsVehicles mrsVehicles = collision.gameObject.GetComponent<MarsVehicles>();

        if (collidable != null)
        {
            pool.ReturnToPool(collision.gameObject);
        }
        //else if (mrsVehicles != null)
        //{
        //    pool.ReturnToPool(collision.gameObject);
        //}

    }
}
