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

    [Header("Visualization")]
    [SerializeField] private AIVisualization visualization;

    [Header("Visualization")]
    [SerializeField] private AttackVisualizer attackVisualizer;

    private Rigidbody rb;
    private Health health;
    private Health playerHealth;
    private float nextAttackTime = 0f;
    private CapsuleCollider playerCollider;
    private Rigidbody playerRb;
    private Animator animator;

    // Attack stats
    private int attacksLanded = 0;
    private int attacksAttempted = 0;

    // Track player movement
    private Vector3 lastPlayerLocalPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();

        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            lastPlayerLocalPosition = player.localPosition;
        }
    }

    public override void OnEpisodeBegin()
    {
        nextAttackTime = 0f;
        attacksLanded = 0;
        attacksAttempted = 0;

        // Reset agent position — slightly tighter range to avoid spawning in walls
        transform.localPosition = new Vector3(
            Random.Range(-8f, 8f),
            1f,
            Random.Range(-8f, 8f)
        );
        transform.rotation = Quaternion.identity;

        // Cache components
        playerCollider = player != null ? player.GetComponent<CapsuleCollider>() : null;
        animator = GetComponent<Animator>();

        if (playerCollider != null)
            playerCollider.enabled = true;

        playerRb = player != null ? player.GetComponent<Rigidbody>() : null;
        if (playerRb != null)
            playerRb.isKinematic = false;

        // Reset agent health
        if (health != null)
            health.ResetHealth();

        if (visualization != null)
        {
            visualization.ResetEpisode();
        }

        // Reset player
        if (player != null)
        {
            player.localPosition = new Vector3(
                Random.Range(-8f, 8f),
                0.3f,
                Random.Range(-8f, 8f)
            );

            if (playerHealth != null)
                playerHealth.ResetHealth();

            lastPlayerLocalPosition = player.localPosition;
        }
        else
        {
            lastPlayerLocalPosition = Vector3.zero;
        }

        FindObjectOfType<TrainingPlayer>()?.ResetForEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent health normalized
        sensor.AddObservation(health != null ? health.CurrentHealth / health.MaxHealth : 0f); // 1

        // Agent local position
        sensor.AddObservation(transform.localPosition);                                        // 3

        if (player != null && playerHealth != null)
        {
            // Player local position
            sensor.AddObservation(player.localPosition);                                       // 3

            // Player health normalized
            sensor.AddObservation(playerHealth.CurrentHealth / playerHealth.MaxHealth);        // 1

            // Direction to player normalized
            Vector3 directionToPlayer = (player.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(directionToPlayer);                                          // 3

            // Distance to player
            float distanceToPlayer = Vector3.Distance(transform.localPosition, player.localPosition);
            sensor.AddObservation(distanceToPlayer);                                           // 1

            // Player movement delta this step
            Vector3 playerMovement = player.localPosition - lastPlayerLocalPosition;
            sensor.AddObservation(playerMovement);                                             // 3
            lastPlayerLocalPosition = player.localPosition;

            // Attack cooldown remaining normalized 0-1
            float cooldownRemaining = Mathf.Max(0f, nextAttackTime - Time.fixedTime);
            sensor.AddObservation(cooldownRemaining / attackCooldown);                         // 1
        }
        else
        {
            sensor.AddObservation(Vector3.zero); // player position    3
            sensor.AddObservation(0f);           // player health      1
            sensor.AddObservation(Vector3.zero); // direction          3
            sensor.AddObservation(0f);           // distance           1
            sensor.AddObservation(Vector3.zero); // player movement    3
            sensor.AddObservation(0f);           // cooldown           1
        }

        // Total = 1 + 3 + 3 + 1 + 3 + 1 + 3 + 1 = 16
        // *** Set VectorObservationSize = 16 in BehaviorParameters ***
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        int attackAction = actions.DiscreteActions[1];

        if (visualization != null)
        {
            string actionName = GetActionName(moveAction, attackAction);
            visualization.UpdateAction(actionName);
        }

        // ---- MOVEMENT ----
        Vector3 moveDirection = Vector3.zero;
        switch (moveAction)
        {
            case 0: break;
            case 1: moveDirection = transform.forward; break; // forward
            case 2: moveDirection = -transform.forward; break; // backward
            case 3: moveDirection = -transform.right; break; // strafe left
            case 4: moveDirection = transform.right; break; // strafe right
        }

        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);

        // ---- ROTATION — always face player ----
        if (player != null)
        {
            Vector3 lookDirection = player.position - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        // ---- ATTACK ----
        if (attackAction == 1)
        {
            TryAttack();
        }

        // ----------------------------------------------------------------
        // REWARDS
        // Philosophy: reward outcomes only (hits, kills).
        // Do NOT reward surviving or standing at a distance —
        // those caused the agent to exploit corners for free reward.
        // Keep all reward values in a tight -1 to +1 per-step range.
        // ----------------------------------------------------------------

        // 1. Step cost — small pressure to act decisively
        AddReward(-0.001f);

        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            // 2. Distance penalty — rubber-band pull toward the player.
            //    Only active outside attack range so the agent closes in.
            //    Scales with distance so being very far is penalized more.
            if (dist > attackRange + 1f)
            {
                AddReward(-0.002f * (dist / 10f));
            }

            // 3. In-range reward — small incentive to stay engaged.
            //    Kept tiny so it is not worth farming without attacking.
            if (dist <= attackRange)
            {
                AddReward(0.002f);
            }

            // 4. Corner/wall penalty — raycast all 4 directions.
            //    If 2 or more walls are nearby the agent is in a corner.
            //    -0.005 per step makes cornering unprofitable immediately.
            int wallCount = 0;
            float wallCheckDist = 1.5f;
            if (Physics.Raycast(transform.position, transform.right, wallCheckDist)) wallCount++;
            if (Physics.Raycast(transform.position, -transform.right, wallCheckDist)) wallCount++;
            if (Physics.Raycast(transform.position, transform.forward, wallCheckDist)) wallCount++;
            if (Physics.Raycast(transform.position, -transform.forward, wallCheckDist)) wallCount++;

            if (wallCount >= 2)
            {
                AddReward(-0.005f);
            }

            // NO survival reward — removed entirely.
            // A per-step survival bonus taught the agent to avoid combat
            // and farm safe idle ticks instead of engaging.
        }
    }

    private void TryAttack()
    {
        if (player == null || playerHealth == null) return;

        // Cooldown — use fixedTime to stay consistent with physics loop
        if (Time.fixedTime < nextAttackTime) return;

        nextAttackTime = Time.fixedTime + attackCooldown;
        attacksAttempted++;

        if (animator != null)
            animator.SetTrigger("Attack");

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool hit = false;

        if (distanceToPlayer <= attackRange)
        {
            // HIT
            playerHealth.TakeDamage(attackDamage);
            attacksLanded++;
            hit = true;

            // Reduced from 1.5 to 1.0 to keep scale tight
            AddReward(1.0f);
            Debug.Log($"[CombatAgent] Hit! Accuracy: {attacksLanded}/{attacksAttempted}");

            if (!playerHealth.IsAlive)
            {
                // Kill reward capped at 2.5 max to prevent reward magnitude explosion
                float accuracyBonus = attacksAttempted > 0
                    ? (float)attacksLanded / attacksAttempted
                    : 0f;

                AddReward(2.0f + accuracyBonus * 0.5f);
                Debug.Log($"[CombatAgent] Player defeated! Accuracy: {accuracyBonus:F2}");
                EndEpisode();
            }
        }
        else
        {
            // MISS — reduced from -0.05 to -0.02 so agent still tries to attack
            AddReward(-0.02f);
            Debug.Log($"[CombatAgent] Miss! Distance: {distanceToPlayer:F2}");
        }

        if (attackVisualizer != null)
        {
            attackVisualizer.OnAttack(hit);
        }
    }

    /// <summary>
    /// Called by Health.cs when this agent takes damage.
    /// lethal = true means this hit brought HP to zero.
    /// </summary>
    public void OnTakeDamage(float damage, bool lethal)
    {
        // Normalized to max health: 20 damage on 100hp = -0.2 penalty
        float penalty = -(damage / (health != null ? health.MaxHealth : 100f));
        AddReward(penalty);
        Debug.Log($"[CombatAgent] Took {damage} damage. Penalty: {penalty:F3}");

        if (lethal)
        {
            // Reduced from -10 to -3 to prevent reward scale explosion
            AddReward(-3f);
            Debug.Log("[CombatAgent] Agent died. Ending episode.");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.W)) discrete[0] = 1;
        else if (Input.GetKey(KeyCode.S)) discrete[0] = 2;
        else if (Input.GetKey(KeyCode.A)) discrete[0] = 3;
        else if (Input.GetKey(KeyCode.D)) discrete[0] = 4;
        else discrete[0] = 0;

        discrete[1] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    string GetActionName(int moveAction, int attackAction)
    {
        string move = moveAction switch
        {
            0 => "Idle",
            1 => "Forward",
            2 => "Backward",
            3 => "Left",
            4 => "Right",
            _ => "Unknown"
        };

        string attack = attackAction == 1 ? " + Attack" : "";

        return move + attack;
    }

    public new void AddReward(float reward)
    {
        base.AddReward(reward);

        if (visualization != null)
        {
            visualization.UpdateReward(reward);
        }
    }
}