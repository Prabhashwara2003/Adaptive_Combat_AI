using UnityEngine;

public class TrainingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform agent;
    [SerializeField] private Health playerHealth;
    [SerializeField] private Health agentHealth;

    [Header("Arena Bounds")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-12f, -12f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(12f, 12f);

    void Start()
    {
        // Subscribe to death events
        if (agentHealth != null)
        {
            // We'll use OnEpisodeBegin in agent instead
        }
    }

    // Called by agent to reset episode
    public void ResetEpisode()
    {
        ResetPlayer();
        ResetAgent();
    }

    void ResetPlayer()
    {
        // Random position
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        player.position = new Vector3(x, 1f, z);

        // Reset health
        playerHealth.currentHealth = playerHealth.MaxHealth;

        // Random rotation
        player.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
    }

    void ResetAgent()
    {
        // Random position (far from player)
        Vector3 playerPos = player.position;
        Vector3 agentPos;

        do
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            agentPos = new Vector3(x, 1f, z);
        }
        while (Vector3.Distance(agentPos, playerPos) < 5f); // Spawn at least 5 units apart

        agent.position = agentPos;

        // Reset health
        agentHealth.currentHealth = agentHealth.MaxHealth;

        // Face player
        Vector3 direction = (playerPos - agentPos).normalized;
        agent.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
    }
}
