using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeSpeed = 15f;
    [SerializeField] private float dodgeDuration = 0.3f;
    [SerializeField] private float dodgeCooldown = 1f;

    private bool isDodging = false;
    private float lastDodgeTime;
    

    private Rigidbody rb;
    private Vector3 moveDirection;

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Freeze rotation so player doesn't tip over
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Get input from keyboard
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows

        // Calculate movement direction
        moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(KeyCode.LeftShift)! & isDodging)
        {
            if (Time.time > lastDodgeTime + dodgeCooldown)
            {
                StartCoroutine(Dodge());
            }
        }
    }

    void FixedUpdate()
    {
        // Move the player
        if (moveDirection.magnitude >= 0.1f)
        {
            if (isDodging) return;

            // Move
            Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);

            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    System.Collections.IEnumerator Dodge()
    {
        isDodging = true;
        lastDodgeTime = Time.time;

        // Make invincible
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetInvincible(true);
        }

        // Dodge in current movement direction (or forward if standing still)
        Vector3 dodgeDirection = moveDirection.magnitude > 0.1f ? moveDirection : transform.forward;

        float elapsedTime = 0f;
        while (elapsedTime < dodgeDuration)
        {
            rb.MovePosition(rb.position + dodgeDirection * dodgeSpeed * Time.fixedDeltaTime);
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Remove invincibility
        if (health != null)
        {
            health.SetInvincible(false);
        }

        isDodging = false;
    }
}