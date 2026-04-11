using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target; // The player
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -20);
    [SerializeField] private float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Look at player (slightly above)
        transform.LookAt(target.position + Vector3.up * 2f);
    }
}