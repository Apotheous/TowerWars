using UnityEngine;

public class ExampleColor : MonoBehaviour
{
    [ColoredHeader("Player Settings", HeaderColor.Blue)]
    public GameObject playerPrefab;

    [ColoredHeader("Enemy Settings", HeaderColor.Red)]
    public GameObject enemyPrefab;

    [ColoredHeader("Pickup Settings", HeaderColor.Green)]
    public GameObject[] pickups;

    [ColoredHeader("UI Elements", HeaderColor.Purple)]
    public Canvas mainCanvas;

    [ColoredHeader("Sound Effects", HeaderColor.Orange)]
    public AudioClip[] soundEffects;
}
