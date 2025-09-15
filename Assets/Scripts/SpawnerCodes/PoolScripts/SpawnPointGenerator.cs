using UnityEngine;
public class SpawnPointGenerator : MonoBehaviour
{

    public enum AxisDirection
    {
        HORİZONTAL,
        VERTİCAL,
        DEPTH
    }

    [ColoredHeader("Spawn Area Settings")]
    [SerializeField] private AxisDirection spawnAxis = AxisDirection.HORİZONTAL;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float deepRadius = 0.5f;
    
    [ColoredHeader("Center Spawn Point", HeaderColor.Red)]
    [ColoredTooltip("Transform Center Where Objects Will Be Spawned",TooltipColor.Green )]
    [SerializeField] private Transform spawnAreaCenterTransform;

    private void Awake()
    {
        if (spawnAreaCenterTransform==null)
        {
            spawnAreaCenterTransform = transform;
        }
    }
    public Vector3 GenerateSpawnPoint()
    {
        return DetermineSpawnPointByAxis(spawnAxis);
    }

    private Vector3 DetermineSpawnPointByAxis(AxisDirection axisType)
    {
        Vector2 randomCirclePoint = Random.insideUnitCircle * radius;

        float randomDepth = Random.Range(-deepRadius, deepRadius);

        switch (axisType)
        {
            case AxisDirection.HORİZONTAL:
                return spawnAreaCenterTransform.position + new Vector3(
                    randomDepth,
                    randomCirclePoint.x,
                    randomCirclePoint.y
                );

            case AxisDirection.VERTİCAL:
                return spawnAreaCenterTransform.position + new Vector3(
                    randomCirclePoint.x,
                    randomDepth,
                    randomCirclePoint.y
                );

            case AxisDirection.DEPTH:
                return spawnAreaCenterTransform.position + new Vector3(
                    randomCirclePoint.x,
                    randomCirclePoint.y,
                    randomDepth
                );

            default:
                Debug.LogWarning("Ge�ersiz eksen. Varsay�lan Horizontal olarak spawn edilecek.");
                return spawnAreaCenterTransform.position + new Vector3(
                    randomDepth,
                    randomCirclePoint.x,
                    randomCirclePoint.y
                );
        }
    }
    private void OnDrawGizmos()
    {
        if (spawnAreaCenterTransform == null) return;

        Gizmos.color = Color.yellow;

        switch (spawnAxis)
        {
            case AxisDirection.HORİZONTAL:
                Gizmos.DrawWireCube(spawnAreaCenterTransform.position, new Vector3(
                    deepRadius * 2,
                    radius * 2,
                    radius * 2
                ));
                break;
            case AxisDirection.VERTİCAL:
                Gizmos.DrawWireCube(spawnAreaCenterTransform.position, new Vector3(
                    radius * 2,
                    deepRadius * 2,
                    radius * 2
                ));
                break;
            case AxisDirection.DEPTH:
                Gizmos.DrawWireCube(spawnAreaCenterTransform.position, new Vector3(
                    radius * 2,
                    radius * 2,
                    deepRadius * 2
                ));
                break;
        }
    }

    public void VisualizeSpawnArea()
    {
        Debug.Log($"Spawn Axis: {spawnAxis}");
        Debug.Log($"Radius: {radius}");
        Debug.Log($"Deep Radius (Thickness): {deepRadius}");
    }
}
