using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI (Optional)")]
    public Slider healthBar;

    [Header("Visual Feedback")]
    public Renderer meshRenderer;
    public float flashDuration = 0.1f;

    private bool isInvincible = false;
    private Material material;
    private Color originalColor;
    private CombatAgent agentComponent;

    // Tracks whether Die() has already been handled this episode
    // to prevent double EndEpisode() calls
    private bool isDead = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
    }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

        if (meshRenderer != null)
        {
            material = meshRenderer.material;
            originalColor = material.color;
        }

        agentComponent = GetComponent<CombatAgent>();
        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        // Don't process damage if already dead or invincible
        if (isInvincible || isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        UpdateHealthBar();

        if (meshRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        if (currentHealth <= 0f)
        {
            isDead = true;

            // Notify agent FIRST — this calls EndEpisode() internally if agent died.
            // We pass isDead=true so the agent knows this hit was lethal.
            if (agentComponent != null)
            {
                agentComponent.OnTakeDamage(damage, lethal: true);
            }

            // Only run Die() (disabling colliders etc.) if this is NOT the agent.
            // The agent resets itself in OnEpisodeBegin, so Die() would break that.
            if (agentComponent == null)
            {
                Die();
            }
        }
        else
        {
            // Non-lethal hit — notify agent for reward shaping
            if (agentComponent != null)
            {
                agentComponent.OnTakeDamage(damage, lethal: false);
            }
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

    IEnumerator DamageFlash()
    {
        if (material != null)
        {
            material.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            material.color = originalColor;
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died!");

        if (TryGetComponent<Collider>(out Collider col))
            col.enabled = false;

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.isKinematic = true;

        if (TryGetComponent<FSMEnemy>(out FSMEnemy ai))
            ai.enabled = false;

        if (TryGetComponent<PlayerController>(out PlayerController pc))
            pc.enabled = false;

        if (meshRenderer != null)
            meshRenderer.material.color = Color.black;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthBar();

        // Safe null check in case ResetHealth is called before Start()
        if (meshRenderer != null)
        {
            // Re-fetch material in case it was swapped during Die()
            material = meshRenderer.material;
            if (material != null)
            {
                material.color = originalColor;
            }
        }

        // Re-enable components that Die() disabled
        if (TryGetComponent<Collider>(out Collider col))
            col.enabled = true;

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.isKinematic = false;

        if (TryGetComponent<FSMEnemy>(out FSMEnemy ai))
            ai.enabled = true;

        if (TryGetComponent<PlayerController>(out PlayerController pc))
            pc.enabled = true;
    }
}