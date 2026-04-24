
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform agent;
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -20);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool focusOnBoth = true;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition;

        if (focusOnBoth && agent != null)
        {
            // Focus on midpoint between player and agent
            Vector3 midpoint = (player.position + agent.position) / 2f;
            targetPosition = midpoint + offset;
        }
        else
        {
            // Focus on player only
            targetPosition = player.position + offset;
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Look at combat area
        Vector3 lookTarget = focusOnBoth && agent != null
            ? (player.position + agent.position) / 2f
            : player.position;

        transform.LookAt(lookTarget + Vector3.up * 2f);
    }
}