using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI (Optional for now)")]
    public Slider healthBar;

    [Header("Visual Feedback")]
    public Renderer meshRenderer;
    public  float flashDuration = 0.1f;

    private bool isInvincible = false;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    private Material material;

    private Color originalColor;

    private CombatAgent agentComponent;

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
    }
    IEnumerator DamageFlash()
    {
        material.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        material.color = originalColor;
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (meshRenderer != null)
        {
            material = meshRenderer.material;
            originalColor = material.color;
        }

        // Get CombatAgent component
        agentComponent = GetComponent<CombatAgent>();

        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0); // Don't go below 0

        UpdateHealthBar();

        // Notify agent if it has one
        if (agentComponent != null)
        {
            agentComponent.OnTakeDamage(damage);
        }

        if (meshRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Don't exceed max
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died!");

        // Disable components
        if (TryGetComponent<Collider>(out Collider col))
            col.enabled = false;

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.isKinematic = true;

        // Disable AI
        if (TryGetComponent<FSMEnemy>(out FSMEnemy ai))
            ai.enabled = false;

        if (TryGetComponent<PlayerController>(out PlayerController pc))
            pc.enabled = false;

        // Visual feedback
        if (meshRenderer != null)
            meshRenderer.material.color = Color.black;

        // Optionally destroy after delay
        //Destroy(gameObject, 3f);

    }
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        // Reset visual if dead
        if (meshRenderer != null)
        {
            material.color = originalColor;
        }
    }

}