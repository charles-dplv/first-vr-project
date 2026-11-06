using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// !! MADE BY CLAUDE TO EASILY TEST THE GAME WITHOUT A HEADSET. IT IS NOT USED IN VR. !!

// Keyboard and mouse player, to test without a headset. It replaces the XR rig when no headset is active.
// The held object follows the hand with velocity tracking, like XRI does in VR,
// so it still hits things and breaks them (it only breaks itself once thrown, see Breakable.IsHeld)
[RequireComponent(typeof(CharacterController))]
public class DesktopPlayer : MonoBehaviour
{
    [SerializeField] private GameObject xrOrigin;
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private Camera playerCamera;
    // Play with keyboard and mouse even if a headset is connected
    [SerializeField] private bool forceDesktop = false;
    [SerializeField] private float moveSpeed = 2.5f;
    // Degrees per pixel of mouse movement
    [SerializeField] private float lookSpeed = 0.1f;
    [SerializeField] private float grabDistance = 3f;
    [SerializeField] private float holdDistance = 1f;
    [SerializeField] private float throwSpeed = 8f;
    // Hand that holds the bat, relative to the camera: position, then angles at rest and at the end of a swing
    [SerializeField] private Vector3 handPosition = new Vector3(0.3f, -0.3f, 0.45f);
    [SerializeField] private Vector3 restAngles = new Vector3(-15f, 20f, 0f);
    [SerializeField] private Vector3 strikeAngles = new Vector3(95f, -30f, 0f);
    private const float strikeTime = 0.15f;
    private const float recoverTime = 0.3f;
    // A blocked object does not try to catch up faster than that (m/s)
    private const float maxFollowSpeed = 15f;

    private CharacterController controller;
    private float pitch;
    private Rigidbody held;
    private Transform heldParent;
    private bool holdsTool;
    // Pose of the grab point in the object (like an attach transform)
    private Vector3 grabLocalPosition;
    private Quaternion grabLocalRotation;
    // Time since the swing started, -1 when not swinging
    private float swingTime = -1f;

    private void Awake()
    {
        // With a headset, the XR rig is used and this player is not needed
        if (XRSettings.isDeviceActive && !forceDesktop)
        {
            gameObject.SetActive(false);
            return;
        }

        xrOrigin.SetActive(false);
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // The swing goes on even if the mouse is freed
        if (swingTime >= 0f)
        {
            swingTime += Time.deltaTime;
            if (swingTime > strikeTime + recoverTime)
                swingTime = -1f;
        }

        if (Keyboard.current == null || Mouse.current == null)
            return;

        // Esc frees the mouse, a click takes it back
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;
            return;
        }

        Look();
        Move();

        // Left click: grab, or swing the bat, or throw the object. Right click: drop
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (held == null)
                TryGrab();
            else if (holdsTool)
                StartSwing();
            else
                Release(playerCamera.transform.forward * throwSpeed);
        }
        if (Mouse.current.rightButton.wasPressedThisFrame && held != null)
            Release(held.linearVelocity);
    }

    private void Look()
    {
        Vector2 mouse = Mouse.current.delta.ReadValue() * lookSpeed;
        // The body turns left and right, only the camera looks up and down
        transform.Rotate(0f, mouse.x, 0f);
        pitch = Mathf.Clamp(pitch - mouse.y, -85f, 85f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        // Keys are read by their place on the keyboard: W A S D on QWERTY is Z Q S D on AZERTY
        Keyboard keyboard = Keyboard.current;
        Vector3 direction = Vector3.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            direction += transform.forward;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            direction -= transform.forward;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            direction += transform.right;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            direction -= transform.right;

        // SimpleMove adds gravity and keeps the player on the ground
        controller.SimpleMove(direction.normalized * moveSpeed);
    }

    private void TryGrab()
    {
        // A thick ray (5 cm sphere): a thin one can pass through the gap between two cubes
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.SphereCast(ray, 0.05f, out RaycastHit hit, grabDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        // The same objects as in VR: the ones with an XR Grab Interactable
        XRGrabInteractable grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
        if (grab == null)
            return;

        held = grab.GetComponent<Rigidbody>();
        heldParent = held.transform.parent;
        // Out of its room while held, so that it can be carried through the detector
        held.transform.SetParent(null, true);
        held.useGravity = false;
        // The default limit (7 rad/s) is too slow for a swing
        held.maxAngularVelocity = 50f;
        SetIgnorePlayer(true);
        SetHeld(true);

        holdsTool = grab.attachTransform != null && grab.attachTransform != grab.transform;
        if (holdsTool)
        {
            // A tool (the bat) is held by its attach point, like in VR
            grabLocalPosition = held.transform.InverseTransformPoint(grab.attachTransform.position);
            grabLocalRotation = Quaternion.Inverse(held.transform.rotation) * grab.attachTransform.rotation;
        }
        else
        {
            // Other objects are held by the point under the crosshair, without turning them
            grabLocalPosition = held.transform.InverseTransformPoint(hit.point);
            grabLocalRotation = Quaternion.Inverse(held.transform.rotation) * playerCamera.transform.rotation;
        }
    }

    private void StartSwing()
    {
        if (swingTime < 0f)
            swingTime = 0f;
    }

    // 0 at rest, goes to 1 during the strike, then back to 0
    private float SwingProgress()
    {
        if (swingTime < 0f)
            return 0f;
        if (swingTime < strikeTime)
            return swingTime / strikeTime;
        return Mathf.Clamp01(1f - (swingTime - strikeTime) / recoverTime);
    }

    private void Release(Vector3 velocity)
    {
        SetIgnorePlayer(false);
        SetHeld(false);
        held.useGravity = true;
        held.linearVelocity = velocity;

        // Back in its room if that one is still shown (or in World for the bat),
        // else in the room where the player is, like RoomManager does in VR
        bool parentShown = heldParent != null && heldParent.gameObject.activeInHierarchy;
        held.transform.SetParent(parentShown ? heldParent : roomManager.CurrentRoom, true);

        held = null;
        swingTime = -1f;
    }

    // The held object must not hit the player's own body: it would push it and break
    private void SetIgnorePlayer(bool ignore)
    {
        foreach (Collider part in held.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(controller, part, ignore);
    }

    // A held object does not break (the bat has no Breakable)
    private void SetHeld(bool isHeld)
    {
        if (held.TryGetComponent(out Breakable breakable))
            breakable.IsHeld = isHeld;
    }

    private void FixedUpdate()
    {
        // Also null when the held object shattered
        if (held == null)
            return;

        // The hand: on the right for the bat (turned by the swing), in front of the camera for the rest
        Transform view = playerCamera.transform;
        Vector3 hand;
        Quaternion handRotation;
        if (holdsTool)
        {
            hand = view.TransformPoint(handPosition);
            handRotation = view.rotation * Quaternion.Slerp(Quaternion.Euler(restAngles), Quaternion.Euler(strikeAngles), SwingProgress());
        }
        else
        {
            hand = view.position + view.forward * GetHoldDistance(view);
            handRotation = view.rotation;
        }

        // Pose of the object that puts its grab point in the hand
        Quaternion targetRotation = handRotation * Quaternion.Inverse(grabLocalRotation);
        Vector3 targetPosition = hand - targetRotation * grabLocalPosition;

        // Velocity tracking: the speeds that reach the target in one physics step
        held.linearVelocity = Vector3.ClampMagnitude((targetPosition - held.position) / Time.fixedDeltaTime, maxFollowSpeed);
        (targetRotation * Quaternion.Inverse(held.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;
        held.angularVelocity = Mathf.Abs(angle) > 0.01f ? axis * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime) : Vector3.zero;
    }

    // Closer when a wall is in the way (about 30 cm in front of it): otherwise the object would be pushed into the wall and break
    private float GetHoldDistance(Transform view)
    {
        float distance = holdDistance;
        foreach (RaycastHit hit in Physics.RaycastAll(view.position, view.forward, holdDistance + 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // Walls and floor are the colliders without a Rigidbody
            if (hit.rigidbody == null)
                distance = Mathf.Min(distance, hit.distance - 0.3f);
        }
        return Mathf.Max(distance, 0.4f);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 20f, 20f), "+");
        GUI.Label(new Rect(10f, 10f, 900f, 22f),
            "ZQSD : avancer   Souris : regarder   Clic gauche : prendre / lancer / frapper   Clic droit : lâcher   Échap : libérer la souris");
    }
}
