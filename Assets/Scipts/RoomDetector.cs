using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoomDetector : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;
    // Head of the player. Uses the main camera if empty: the XR camera or the desktop one,
    // so it is looked up every frame (we only know at start which one is used)
    [SerializeField] private Transform player;
    private const float deadZone = 0.05f;
    private BoxCollider box;
    private int lastSide;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void Update()
    {
        Transform head = player != null ? player : (Camera.main != null ? Camera.main.transform : null);
        if (head == null)
            return;

        Vector3 local = transform.InverseTransformPoint(head.position) - box.center;

        // Forget the side as soon as the player leaves the detector (height is ignored)
        if (Mathf.Abs(local.x) > box.size.x / 2f || Mathf.Abs(local.z) > box.size.z / 2f)
        {
            lastSide = 0;
            return;
        }

        int side = GetSide(local.z);

        // Change room as soon as the player crosses the detector plane
        if (side != 0 && lastSide != 0 && side != lastSide)
            roomManager.ChangeRoom(side);

        if (side != 0)
            lastSide = side;
    }

    // 1 if in front of the detector (blue axis), -1 if behind, 0 if on the plane
    private int GetSide(float localZ)
    {
        float d = localZ * transform.lossyScale.z;
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
    }
}
