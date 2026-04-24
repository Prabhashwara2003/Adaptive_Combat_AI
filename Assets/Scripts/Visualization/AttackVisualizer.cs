using UnityEngine;
using System.Collections.Generic;

public class AttackVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem attackEffect;
    [SerializeField] private LineRenderer attackRangeLine;

    [Header("Settings")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private Color rangeColor = Color.red;
    [SerializeField] private bool showAttackRange = true;

    [Header("Stats Tracking")]
    [SerializeField] private bool trackStats = true;

    private int totalAttacks = 0;
    private int successfulHits = 0;
    private List<Vector3> attackPositions = new List<Vector3>();

    void Start()
    {
        SetupRangeIndicator();
    }

    void SetupRangeIndicator()
    {
        if (attackRangeLine == null)
        {
            // Create LineRenderer for attack range
            GameObject rangeObj = new GameObject("Attack_Range_Indicator");
            rangeObj.transform.parent = transform;
            rangeObj.transform.localPosition = Vector3.zero;

            attackRangeLine = rangeObj.AddComponent<LineRenderer>();
            attackRangeLine.material = new Material(Shader.Find("Sprites/Default"));
            attackRangeLine.startColor = rangeColor;
            attackRangeLine.endColor = rangeColor;
            attackRangeLine.startWidth = 0.05f;
            attackRangeLine.endWidth = 0.05f;
            attackRangeLine.useWorldSpace = false;

            // Create circle
            int segments = 50;
            attackRangeLine.positionCount = segments + 1;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 360f / segments * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * attackRange;
                float z = Mathf.Sin(angle) * attackRange;
                attackRangeLine.SetPosition(i, new Vector3(x, 0.1f, z));
            }
        }

        attackRangeLine.gameObject.SetActive(showAttackRange);
    }

    public void OnAttack(bool hit)
    {
        // Play particle effect
        if (attackEffect != null)
        {
            attackEffect.Play();
        }

        // Track stats
        if (trackStats)
        {
            totalAttacks++;
            if (hit)
            {
                successfulHits++;
                attackPositions.Add(transform.position);
            }
        }
    }

    public float GetHitAccuracy()
    {
        if (totalAttacks == 0) return 0f;
        return (float)successfulHits / totalAttacks * 100f;
    }

    public void ResetStats()
    {
        totalAttacks = 0;
        successfulHits = 0;
        attackPositions.Clear();
    }

    public void ToggleRangeIndicator()
    {
        showAttackRange = !showAttackRange;
        if (attackRangeLine != null)
        {
            attackRangeLine.gameObject.SetActive(showAttackRange);
        }
    }

    void OnDrawGizmos()
    {
        // Draw attack positions
        Gizmos.color = Color.yellow;
        foreach (Vector3 pos in attackPositions)
        {
            Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }
}