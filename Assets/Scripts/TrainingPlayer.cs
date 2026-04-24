using UnityEngine;

public class TrainingPlayer : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private DummyMode mode = DummyMode.Stationary;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float wanderRadius = 8f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("References")]
    [SerializeField] private Transform agent;

    // agentHealth is fetched automatically from the agent Transform — no need to assign in Inspector
    private Health agentHealth;
    private Health health;
    private Rigidbody rb;

    private float lastAttackTime;
    private Vector3 wanderTarget;
    private Vector3 startPosition;

    public enum DummyMode
    {
        Stationary,  // Stands still
        Wander,      // Moves randomly
        ChaseAgent,  // Follows agent
        FightBack    // Chases and attacks agent
    }

    void Start()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        wanderTarget = transform.position;

        // Auto-fetch agent health so it never goes stale
        if (agent != null)
        {
            agentHealth = agent.GetComponent<Health>();
        }
    }

    void Update()
    {
        if (health == null || !health.IsAlive || agent == null) return;

        // Refresh agentHealth reference in case agent was reset
        if (agentHealth == null)
        {
            agentHealth = agent.GetComponent<Health>();
        }

        switch (mode)
        {
            case DummyMode.Stationary:
                break;
            case DummyMode.Wander:
                Wander();
                break;
            case DummyMode.ChaseAgent:
                ChaseAgent();
                break;
            case DummyMode.FightBack:
                FightBack();
                break;
        }
    }

    void Wander()
    {
        if (Vector3.Distance(transform.position, wanderTarget) < 1f)
        {
            // Pick a new wander target clamped to wander radius around start position
            // so the dummy doesn't drift off the map
            Vector3 offset = new Vector3(
                Random.Range(-wanderRadius, wanderRadius),
                0f,
                Random.Range(-wanderRadius, wanderRadius)
            );
            wanderTarget = startPosition + offset;
        }

        MoveTowards(wanderTarget);
    }

    void ChaseAgent()
    {
        MoveTowards(agent.position);
        FaceTarget(agent.position);
    }

    void FightBack()
    {
        float distance = Vector3.Distance(transform.position, agent.position);

        if (distance > attackRange)
        {
            ChaseAgent();
        }
        else
        {
            FaceTarget(agent.position);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackAgent();
                lastAttackTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Moves using Rigidbody if available, otherwise falls back to transform.
    /// Using Rigidbody prevents clipping through colliders.
    /// </summary>
    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f;

        if (rb != null)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 lookTarget = new Vector3(target.x, transform.position.y, target.z);
        transform.LookAt(lookTarget);
    }

    void AttackAgent()
    {
        if (agentHealth == null || !agentHealth.IsAlive) return;

        agentHealth.TakeDamage(attackDamage);
        Debug.Log("[TrainingPlayer] Attacked agent!");
    }

    /// <summary>
    /// Call this from your episode manager or CombatAgent.OnEpisodeBegin()
    /// to reset the dummy at the start of each training episode.
    /// </summary>
    public void ResetForEpisode()
    {
        lastAttackTime = 0f;
        wanderTarget = startPosition;

        // Re-fetch agent health in case the agent object was re-initialized
        if (agent != null)
        {
            agentHealth = agent.GetComponent<Health>();
        }

        // Snap back to start position if using Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetMode(DummyMode newMode)
    {
        mode = newMode;
        Debug.Log($"[TrainingPlayer] Mode changed to: {mode}");
    }
}