using UnityEngine;

public class FSMEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform weaponTip;

    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float retreatHealthPercent = 0.3f; // Retreat at 30% health

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Debug Visualization")]
    [SerializeField] private Renderer stateIndicator;
    [SerializeField] private Material idleMat;
    [SerializeField] private Material chaseMat;
    [SerializeField] private Material attackMat;
    [SerializeField] private Material retreatMat;

    private EnemyState currentState;
    private Rigidbody rb;
    private Health health;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        currentState = EnemyState.Idle;

        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    void Update()
    {
        if (player == null || !health.IsAlive) return;

        // Decide state based on conditions
        DecideState();
        Debug.Log("Current State: " + currentState);
        // Execute current state behavior
        ExecuteState();
    }

    void DecideState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        // Priority: Retreat if low health
        if (healthPercent <= retreatHealthPercent)
        {
            currentState = EnemyState.Retreat;
        }
        // Attack if in range
        else if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        // Chase if player detected
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        // Otherwise idle
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    void ExecuteState()
    {
        // Update visual indicator
        UpdateStateIndicator();

        switch (currentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Chase:
                Chase();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Retreat:
                Retreat();
                break;
        }
    }

    void Idle()
    {
        // Just stand still
        // Could add: look around, random walking, etc.
    }

    void Chase()
    {
        // Move toward player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Stay on ground

        // Move
        rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);

        // Rotate toward player
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Attack()
    {
        // Face player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Try to attack
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    void Retreat()
    {
        // Move away from player
        Vector3 direction = (transform.position - player.position).normalized;
        direction.y = 0;

        rb.MovePosition(rb.position + direction * moveSpeed * 0.7f * Time.deltaTime); // Slower retreat

        // Face player while retreating
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void PerformAttack()
    {
        Debug.Log("Enemy attacked!");

        // Find player in range
        Collider[] hitColliders = Physics.OverlapSphere(weaponTip.position, attackRange);

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Player") && col.TryGetComponent<Health>(out Health playerHealth))
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    // Visualize ranges in editor
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void UpdateStateIndicator()
    {
        if (stateIndicator == null) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                stateIndicator.material = idleMat;
                break;
            case EnemyState.Chase:
                stateIndicator.material = chaseMat;
                break;
            case EnemyState.Attack:
                stateIndicator.material = attackMat;
                break;
            case EnemyState.Retreat:
                stateIndicator.material = retreatMat;
                break;
        }
    }
}