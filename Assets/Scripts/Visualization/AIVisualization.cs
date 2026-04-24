using UnityEngine;
using TMPro;

public class AIVisualization : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Health agentHealth;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI actionText;

    [Header("Graph")]
    [SerializeField] private RewardGraph rewardGraph;

    [Header("Settings")]
    [SerializeField] private bool showVisualization = true;

    private float episodeReward = 0f;
    private string currentAction = "None";
    private string currentState = "Idle";

    void Update()
    {
        if (!showVisualization) return;

        UpdateVisualization();
    }

    void UpdateVisualization()
    {
        if (agent == null || player == null) return;

        // Calculate distance
        float distance = Vector3.Distance(agent.transform.position, player.position);
        distanceText.text = $"{distance:F2}m";

        // Update health
        float healthPercent = (agentHealth.currentHealth / agentHealth.MaxHealth) * 100f;
        healthText.text = $"{healthPercent:F0}%";

        // Color code health
        if (healthPercent > 70)
            healthText.color = Color.green;
        else if (healthPercent > 30)
            healthText.color = Color.yellow;
        else
            healthText.color = Color.red;

        // Determine state based on distance and health
        if (!agentHealth.IsAlive)
        {
            currentState = "DEAD";
            stateText.color = Color.red;
        }
        else if (healthPercent < 30)
        {
            currentState = "RETREATING";
            stateText.color = Color.yellow;
        }
        else if (distance <= 2.5f)
        {
            currentState = "ATTACKING";
            stateText.color = Color.red;
        }
        else if (distance <= 10f)
        {
            currentState = "CHASING";
            stateText.color = Color.yellow;
        }
        else
        {
            currentState = "IDLE";
            stateText.color = Color.cyan;
        }

        stateText.text = currentState;

        // Update reward (we'll connect this from agent)
        rewardText.text = $"{episodeReward:F1}";

        // Color code reward
        if (episodeReward > 0)
            rewardText.color = Color.green;
        else if (episodeReward < 0)
            rewardText.color = Color.red;
        else
            rewardText.color = Color.white;

        // Update action
        actionText.text = currentAction;
    }

    // Called from CombatAgent to update reward
    public void UpdateReward(float reward)
    {
        episodeReward += reward;
        if (rewardGraph != null)
        {
            rewardGraph.AddDataPoint(episodeReward);
        }

    }

    // Called from CombatAgent to update action
    public void UpdateAction(string action)
    {
        currentAction = action;
    }

    // Called from CombatAgent on episode begin
    public void ResetEpisode()
    {
        episodeReward = 0f;
        currentAction = "None";
    }

    // Toggle visualization on/off
    public void ToggleVisualization()
    {
        showVisualization = !showVisualization;
        gameObject.SetActive(showVisualization);
    }
}