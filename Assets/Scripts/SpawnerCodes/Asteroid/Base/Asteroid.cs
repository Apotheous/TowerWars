using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Asteroid : MonoBehaviour,ICollidable
{
    public AsteroidType asteroidType;



    public float my_Score;
    public float my_Damage;


    [SerializeField]
    private float collisionDamage = 10f;

    private void Start()
    {
        SetAsteroidProperties(asteroidType);

    }



    public void IDie()
    {
        //GameManager.instance.scoreVal += 50;
        Destroy(this.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        
    }

    private void SetAsteroidProperties(AsteroidType asteroidType)
    {
        switch (asteroidType)
        {
            case AsteroidType.Asteroid:
                
                
                break;
            case AsteroidType.Mine:
                if (gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().enabled == false)
                {
                    gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
                }
                

                break;
            default:
                break;
        }
    }



    public void OnCollide(GameObject collObj)
    {
        switch (asteroidType)
        {
            case AsteroidType.Asteroid:

                //collObj.GetComponent<SaturnPlayerSC>().UpdateHealth(my_Damage);
                break;
            case AsteroidType.Mine:

                //collObj.GetComponent<SaturnPlayerSC>().UpdateScore(my_Score);
                gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                break; 
            case AsteroidType.Elements:

                //collObj.GetComponent<PlayerSC>().UpdateScore(my_Score);
                //gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                break;

            default:
                break;
        }
    }
}

public enum AsteroidType
{
    Asteroid,
    Mine,
    Elements
}

