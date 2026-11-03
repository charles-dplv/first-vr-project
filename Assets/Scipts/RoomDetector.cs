using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoomDetector : MonoBehaviour
{
    
    [SerializeField] private RoomManager roomManager;
    private const float deadZone = 0.05f;
    private int lastSide;
    private int playerColliders;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // The player can have several colliders: only remember the side for the first one
        if (playerColliders++ == 0)
            lastSide = GetSide(other.bounds.center);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        int side = GetSide(other.bounds.center);

        // Change room as soon as the player crosses the detector plane
        if (side != 0 && lastSide != 0 && side != lastSide)
            roomManager.ChangeRoom(side);

        if (side != 0)
            lastSide = side;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerColliders = Mathf.Max(0, playerColliders - 1);
    }

    // 1 if the position is in front of the detector (blue axis), -1 if behind, 0 if on the plane
    private int GetSide(Vector3 position)
    {
        float d = Vector3.Dot(position - transform.TransformPoint(GetComponent<BoxCollider>().center), transform.forward);
        return Mathf.Abs(d) < deadZone ? 0 : (d > 0 ? 1 : -1);
    }

    private void OnDrawGizmos()
    {
        if (!TryGetComponent(out BoxCollider box))
            return;

        // Detector zone
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.center, box.size);

        // Arrow pointing toward the next room
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 start = transform.TransformPoint(box.center);
        Vector3 end = start + transform.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }
}
