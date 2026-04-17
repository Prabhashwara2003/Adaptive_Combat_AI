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

    private Transform agent;
    private Health health;
    private float lastAttackTime;
    private Vector3 wanderTarget;

    public enum DummyMode
    {
        Stationary,      // Doesn't move
        Wander,          // Moves randomly
        ChaseAgent,      // Follows agent
        FightBack        // Attacks agent
    }

    void Start()
    {
        health = GetComponent<Health>();
        agent = GameObject.FindGameObjectWithTag("Agent")?.transform;
        wanderTarget = transform.position;
    }


    void Update()
    {
        if (!health.IsAlive || agent == null) return;

        switch (mode)
        {
            case DummyMode.Stationary:
                // Do nothing
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
        // Move to random positions
        if (Vector3.Distance(transform.position, wanderTarget) < 1f)
        {
            // Pick new target
            wanderTarget = transform.position + new Vector3(
                Random.Range(-wanderRadius, wanderRadius),
                0,
                Random.Range(-wanderRadius, wanderRadius)
            );
        }

        Vector3 direction = (wanderTarget - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    void ChaseAgent()
    {
        Vector3 direction = (agent.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Face agent
        transform.LookAt(new Vector3(agent.position.x, transform.position.y, agent.position.z));
    }

    void FightBack()
    {
        float distance = Vector3.Distance(transform.position, agent.position);

        if (distance > attackRange + 1f)
        {
            // Chase if too far
            ChaseAgent();
        }
        else if (distance <= attackRange)
        {
            // In range - attack
            transform.LookAt(new Vector3(agent.position.x, transform.position.y, agent.position.z));

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackAgent();
                lastAttackTime = Time.time;
            }
        }
    }

    void AttackAgent()
    {
        if (agent.TryGetComponent<Health>(out Health agentHealth))
        {
            agentHealth.TakeDamage(attackDamage);
            Debug.Log("Dummy attacked agent!");
        }
    }

    // Change mode during training
    public void SetMode(DummyMode newMode)
    {
        mode = newMode;
        Debug.Log($"Training dummy mode: {mode}");
    }
}