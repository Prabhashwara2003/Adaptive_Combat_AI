using UnityEngine;

public class AnimationStateControl : MonoBehaviour
{
    private Animator animator;
    private Vector3 lastPosition;

    [SerializeField] private float moveThreshold = 0.01f; // Linear speed (units/sec)
    [SerializeField] private float stopDelay = 0.1f;      // Seconds before switching to idle

    private float stopTimer = 0f;
    private bool isMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Use speed (distance/time) instead of raw delta — frame-rate independent
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        if (speed > moveThreshold)
        {
            stopTimer = 0f;
            if (!isMoving)
            {
                isMoving = true;
                animator.SetBool("IsWalking", true);
            }
        }
        else
        {
            // Only switch to idle after stopDelay seconds of no movement
            stopTimer += Time.deltaTime;
            if (isMoving && stopTimer >= stopDelay)
            {
                isMoving = false;
                animator.SetBool("IsWalking", false);
            }
        }

        lastPosition = transform.position;
    }
}