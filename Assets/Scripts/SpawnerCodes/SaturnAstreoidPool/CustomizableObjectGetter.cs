using System.Collections;
using UnityEngine;

public class CustomizableObjectGetter : MonoBehaviour
{
    [SerializeField] private SaturnAsteroidPoolCreating myPool;

    [SerializeField] private SpawnPointGenerator spawnPointGenerator;

    [ColoredHeader("Parent Gameobject Where You May Want To Send Objects", HeaderColor.Red)]
    [SerializeField] private GameObject asteroidsMain;

    [SerializeField] private int spawnPointCount = 10;

    [ColoredHeader("Spawn Behavior")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float stopCorutineSec;

    private WaitForSeconds _waitTime;
    private Coroutine _spawnCoroutine;


    private void Start()
    {
        _waitTime = new WaitForSeconds(spawnInterval);
        StartCoroutine(SpawnRoutine());
    }

    private void OnEnable()
    {
        _waitTime = new WaitForSeconds(spawnInterval);
        StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    private IEnumerator SpawnRoutine()
    {

        if (stopCorutineSec > 0)
        {
            float elapsedTime = 0f;

            while (elapsedTime < stopCorutineSec)
            {
                yield return _waitTime;
                GetAsteroids();

                elapsedTime += spawnInterval;
            }
        }

        else
        {
            while (true)
            {
                yield return _waitTime;
                GetAsteroids();
            }
        }
    }

    public void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
    }
    private void GetAsteroids()
    {
        int xcf = Random.Range(0, spawnPointCount);
        var new_Aste = myPool.GetFromPool();

        new_Aste.transform.position = spawnPointGenerator.GenerateSpawnPoint();
        if (asteroidsMain != null) new_Aste.transform.SetParent(asteroidsMain.transform);
    }
}
