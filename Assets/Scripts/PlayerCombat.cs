using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private Transform weaponTip; // Where attack originates

    private float lastAttackTime;

    System.Collections.IEnumerator AttackAnimation()
    {
        Transform weapon = weaponTip.GetChild(0); // The weapon cube
        Vector3 originalScale = weapon.localScale;

        // Swing forward
        weapon.localScale = originalScale * 1.5f;
        yield return new WaitForSeconds(0.1f);

        // Return to normal
        weapon.localScale = originalScale;
    }

    void Update()
    {
        // Attack with Spacebar or Left Mouse Button
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }

        
    }



    void TryAttack()
    {
        // Check cooldown
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return; // Still on cooldown
        }

        lastAttackTime = Time.time;
        PerformAttack();
    }

    void PerformAttack()
    {
        if (weaponTip != null)
        {
            StartCoroutine(AttackAnimation());
        }

        Debug.Log("Player attacked!");

        

        // Find enemies in range
        Collider[] hitColliders = Physics.OverlapSphere(weaponTip.position, attackRange);

        foreach (Collider col in hitColliders)
        {
            // Check if hit object has Health component and is not the player
            if (col.gameObject != gameObject && col.TryGetComponent<Health>(out Health health))
            {
                health.TakeDamage(attackDamage);
                Debug.Log("Hit: " + col.gameObject.name);
            }
        }
    }

    // Visualize attack range in editor
    void OnDrawGizmosSelected()
    {
        if (weaponTip == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(weaponTip.position, attackRange);
    }
}