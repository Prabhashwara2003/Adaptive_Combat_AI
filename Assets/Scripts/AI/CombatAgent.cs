using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CombatAgent : Agent
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform weaponTip;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 1f;

    private Rigidbody rb;
    private Health health;
    private Health playerHealth;
    private float nextAttackTime = 0f;
    private CapsuleCollider playerCollider;
    private Rigidbody playerRb;

    // Attack stats
    private int attacksLanded = 0;
    private int attacksAttempted = 0;

    // Track player movement
    private Vector3 lastPlayerPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();

        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            lastPlayerPosition = player.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        nextAttackTime = 0f;
        attacksLanded = 0;
        attacksAttempted = 0;

        // Reset agent position
        transform.localPosition = new Vector3(
            Random.Range(-10f, 10f),
            1f,
            Random.Range(-10f, 10f)
        );
        
        // Reset rotation
        transform.rotation = Quaternion.identity;

        playerCollider = player.GetComponent<CapsuleCollider>();

        if (playerCollider  != null)
        {
            playerCollider.enabled = true;
        }

        playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
        }

        // Reset health
        if (health != null)
        {
            health.ResetHealth();
        }

        // Reset player
        if (player != null)
        {
            player.localPosition = new Vector3(
                Random.Range(-10f, 10f),
                0.3f,
                Random.Range(-10f, 10f)
            );

            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            lastPlayerPosition = player.position;
        }
        else
        {
            lastPlayerPosition = Vector3.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent data
        sensor.AddObservation(health.CurrentHealth / health.MaxHealth);   // 1
        sensor.AddObservation(transform.localPosition);                   // 3

        if (player != null && playerHealth != null)
        {
            // Player data
            sensor.AddObservation(player.localPosition);                  // 3
            sensor.AddObservation(playerHealth.CurrentHealth / playerHealth.MaxHealth); // 1

            Vector3 directionToPlayer = player.localPosition - transform.localPosition;
            sensor.AddObservation(directionToPlayer.normalized);          // 3

            sensor.AddObservation(Vector3.Distance(transform.localPosition, player.localPosition)); // 1

            // New: player movement
            Vector3 playerMovement = player.position - lastPlayerPosition;
            sensor.AddObservation(playerMovement);                        // 3
            lastPlayerPosition = player.position;
        }
        else
        {
            sensor.AddObservation(Vector3.zero); // player position        // 3
            sensor.AddObservation(0f);           // player health          // 1
            sensor.AddObservation(Vector3.zero); // direction to player    // 3
            sensor.AddObservation(0f);           // distance               // 1
            sensor.AddObservation(Vector3.zero); // player movement        // 3
        }

        // Total = 1 + 3 + 3 + 1 + 3 + 1 + 3 = 15
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        int attackAction = actions.DiscreteActions[1];

        // MOVEMENT
        Vector3 moveDirection = Vector3.zero;
        switch (moveAction)
        {
            case 0:
                break;
            case 1:
                moveDirection = transform.forward;
                break;
            case 2:
                moveDirection = -transform.forward;
                break;
            case 3:
                moveDirection = -transform.right;
                break;
            case 4:
                moveDirection = transform.right;
                break;
        }

        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);

        // ROTATION
        if (player != null)
        {
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0f;

            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        // ATTACK
        if (attackAction == 1)
        {
            TryAttack();
        }

        AddReward(-0.001f);

        // DISTANCE REWARD
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float optimalDistance = 3f;
            float distanceFromOptimal = Mathf.Abs(distanceToPlayer - optimalDistance);

            if (distanceFromOptimal < 1f)
            {
                AddReward(0.01f);
            }
            else if (distanceToPlayer < 1f)
            {
                AddReward(-0.02f);
            }
            else if (distanceToPlayer > 10f)
            {
                AddReward(-0.01f);
            }
        }

        // SURVIVAL REWARD
        if (health != null && health.IsAlive)
        {
            AddReward(0.001f);
        }

        // Time penalty
        AddReward(-0.0005f);
    }

    void TryAttack()
    {
        if (player == null || playerHealth == null) return;

        // Cooldown check
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        attacksAttempted++;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            // HIT
            playerHealth.TakeDamage(attackDamage);
            attacksLanded++;

            AddReward(1.5f);

            Debug.Log($"Agent hit! Accuracy: {attacksLanded}/{attacksAttempted}");

            if (!playerHealth.IsAlive)
            {
                float accuracyBonus = attacksAttempted > 0
                    ? (float)attacksLanded / attacksAttempted
                    : 0f;

                AddReward(5.0f + accuracyBonus * 2f);
                EndEpisode();
            }
        }
        else
        {
            // MISS
            AddReward(-0.3f);
            Debug.Log($"Agent missed! Distance: {distanceToPlayer:F2}");
        }
    }

    public void OnTakeDamage(float damage)
    {
        float penalty = -damage / 10f;
        AddReward(penalty);

        Debug.Log($"Agent took {damage} damage! Penalty: {penalty}");

        if (health != null && !health.IsAlive)
        {
            AddReward(-10.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.W))
            discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.S))
            discreteActions[0] = 2;
        else if (Input.GetKey(KeyCode.A))
            discreteActions[0] = 3;
        else if (Input.GetKey(KeyCode.D))
            discreteActions[0] = 4;
        else
            discreteActions[0] = 0;

        discreteActions[1] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}