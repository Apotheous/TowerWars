using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    #region ClaudeCode
    [Header("Movement Settings")]
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float targetDistance = 1000f;

    [Tooltip("Use FixedUpdate instead of Update for physics-based movement")]
    [SerializeField] private bool useFixedUpdate = false;

    [Header("References")]
    public Transform player;

    private Vector3 velocity;
    private Transform cachedTransform;
    private bool hasReachedDistance = false;

    private void Awake()
    {
        cachedTransform = transform;
        UpdateVelocity();
    }

    private void Update()
    {
        if (!useFixedUpdate)
        {
            HandleMovement(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            HandleMovement(Time.fixedDeltaTime);
        }
    }

    private void HandleMovement(float deltaTime)
    {
        if (player == null) return;

        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (currentDistance < targetDistance)
        {
            // Henüz hedef mesafeye ulaþýlmadýysa, moveSpeed ile uzaklaþ
            hasReachedDistance = false;
            Move(deltaTime);
        }
        else
        {
            // Hedef mesafeye ulaþýldýysa, mesafeyi koru
            hasReachedDistance = true;
            MaintainDistance();
        }
    }

    private void Move(float deltaTime)
    {
        cachedTransform.position += velocity * deltaTime;
    }

    private void MaintainDistance()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        transform.position = player.position + direction * targetDistance;
    }

    private void UpdateVelocity()
    {
        velocity = moveDirection.normalized * moveSpeed;
    }

    private void OnValidate()
    {
        UpdateVelocity();
    }

    // Görsel hata ayýklama için
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, targetDistance);
        }
    }
    #endregion

}
