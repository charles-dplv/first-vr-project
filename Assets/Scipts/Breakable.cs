using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Object made of small cubes (children with a collider) held together by one Rigidbody.
// A hit that is fast and strong enough detaches the cubes close to the impact,
// then the parts that no longer touch the rest fall off as separate objects
[RequireComponent(typeof(Rigidbody))]
public class Breakable : MonoBehaviour
{
    [SerializeField] private float minSpeed = 3f;
    [SerializeField] private float minImpulse = 2f;
    [SerializeField] private float breakRadius = 0.25f;
    [SerializeField] private float chunkMass = 0.2f;
    // Speed (m/s) of the detached cubes away from the impact
    [SerializeField] private float explosionSpeed = 2f;
    // Seconds before a detached cube is destroyed
    [SerializeField] private float chunkLifetime = 5f;
    // A part with fewer cubes falls apart instead of becoming a new object
    private const int minPieceSize = 4;

    private readonly List<Transform> chunks = new List<Transform>();
    private Rigidbody body;
    private MeshFilter meshFilter;
    private Mesh combinedMesh;
    private bool shatters;

    // Set by DesktopPlayer while the object is held: with a mouse the "hand" is hard to control
    // (a quick turn slams the object into a wall), so in desktop mode an object only breaks once thrown
    public bool IsHeld { get; set; }

    public void Setup(float minSpeed, float minImpulse, float breakRadius, float chunkMass)
    {
        this.minSpeed = minSpeed;
        this.minImpulse = minImpulse;
        this.breakRadius = breakRadius;
        this.chunkMass = chunkMass;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        // The cubes are the children with a collider (XRI also adds an attach point as a child when grabbing)
        foreach (Transform child in transform)
            if (child.TryGetComponent(out Collider _))
                chunks.Add(child);

        // A grabbable object shatters completely. The XR Interaction Manager links each collider to the object
        // when it registers, so if a part stayed, grabbing a fallen cube would grab that part
        shatters = TryGetComponent(out XRGrabInteractable _);

        // The cubes are drawn by one mesh on this object (see UpdateObject)
        if (!TryGetComponent(out meshFilter))
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if (!TryGetComponent(out MeshRenderer meshRenderer))
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (chunks.Count > 0)
            meshRenderer.sharedMaterial = chunks[0].GetComponent<MeshRenderer>().sharedMaterial;

        UpdateObject();
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        // Speed at which the two objects come together, along the normal of the contact
        float speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));

        // Impulse that stops them: J = μ v, with μ = m1 m2 / (m1 + m2) the reduced mass.
        // A wall or a kinematic body counts as an infinite mass, then μ = m1.
        // We compute it instead of reading collision.impulse: Unity splits that one between the cubes,
        // and gives 0 when continuous collision detection stops the object just before the contact
        Rigidbody other = collision.rigidbody;
        float reducedMass = other == null || other.isKinematic ? body.mass : body.mass * other.mass / (body.mass + other.mass);
        float impulse = reducedMass * speed;

        if (IsHeld || speed < minSpeed || impulse < minImpulse)
            return;

        Break(contact.point);
    }

    private void Break(Vector3 impact)
    {
        // 1. Detach the cubes close to the impact (all of them if the object shatters)
        // Go backwards because we remove cubes from the list
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            if (shatters || Vector3.Distance(chunks[i].position, impact) <= breakRadius)
            {
                Detach(chunks[i], impact);
                chunks.RemoveAt(i);
            }
        }

        // 2. Group the remaining cubes into parts that touch each other
        List<List<Transform>> parts = FindParts();
        if (parts.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // 3. The biggest part stays this object, the other ones fall off
        parts.Sort((a, b) => b.Count.CompareTo(a.Count));
        chunks.Clear();
        chunks.AddRange(parts[0]);
        for (int i = 1; i < parts.Count; i++)
        {
            if (parts[i].Count < minPieceSize)
            {
                foreach (Transform chunk in parts[i])
                    Detach(chunk, impact);
            }
            else
            {
                CreatePiece(parts[i]);
            }
        }

        UpdateObject();
    }

    // Flood fill (breadth-first search): start a part with any cube, then add every cube that touches a cube of the part
    private List<List<Transform>> FindParts()
    {
        List<List<Transform>> parts = new List<List<Transform>>();
        List<Transform> left = new List<Transform>(chunks);

        while (left.Count > 0)
        {
            List<Transform> part = new List<Transform> { left[0] };
            left.RemoveAt(0);

            // The part grows while we go through it, it is also the queue of the search
            for (int i = 0; i < part.Count; i++)
            {
                for (int j = left.Count - 1; j >= 0; j--)
                {
                    if (AreTouching(part[i], left[j]))
                    {
                        part.Add(left[j]);
                        left.RemoveAt(j);
                    }
                }
            }
            parts.Add(part);
        }
        return parts;
    }

    // Overlap test of two axis-aligned boxes (the cubes are not rotated inside the object),
    // with a 10% margin for the gap between two cubes
    private static bool AreTouching(Transform a, Transform b)
    {
        Vector3 distance = a.localPosition - b.localPosition;
        Vector3 contact = (a.localScale + b.localScale) / 2f * 1.1f;
        return Mathf.Abs(distance.x) < contact.x && Mathf.Abs(distance.y) < contact.y && Mathf.Abs(distance.z) < contact.z;
    }

    private void Detach(Transform chunk, Vector3 impact)
    {
        // Keep the cube in the room, so it is hidden along with it
        chunk.SetParent(transform.parent, true);
        chunk.GetComponent<MeshRenderer>().enabled = true;

        Rigidbody chunkBody = chunk.gameObject.AddComponent<Rigidbody>();
        chunkBody.mass = chunkMass;
        // The cube keeps the speed it had as a part of the object, plus a push away from the impact
        Vector3 away = (chunk.position - impact).normalized;
        chunkBody.linearVelocity = body.GetPointVelocity(chunk.position) + away * explosionSpeed;

        Destroy(chunk.gameObject, chunkLifetime);
    }

    // A part that fell off becomes a new breakable object, with its own Rigidbody
    private void CreatePiece(List<Transform> part)
    {
        GameObject piece = new GameObject(name);
        // Inactive while it is set up, so that its Awake runs after Setup
        piece.SetActive(false);
        piece.transform.SetParent(transform.parent, false);
        piece.transform.SetPositionAndRotation(transform.position, transform.rotation);
        foreach (Transform chunk in part)
            chunk.SetParent(piece.transform, true);

        piece.AddComponent<Breakable>().Setup(minSpeed, minImpulse, breakRadius, chunkMass);
        piece.SetActive(true);

        // The piece keeps the motion it had as a part of the object
        Rigidbody pieceBody = piece.GetComponent<Rigidbody>();
        pieceBody.linearVelocity = body.linearVelocity;
        pieceBody.angularVelocity = body.angularVelocity;
    }

    // Mass of the remaining cubes, and one mesh that draws them all:
    // one draw call per object instead of one per cube, which matters on Quest
    private void UpdateObject()
    {
        body.mass = chunkMass * chunks.Count;

        CombineInstance[] meshes = new CombineInstance[chunks.Count];
        for (int i = 0; i < chunks.Count; i++)
        {
            meshes[i].mesh = chunks[i].GetComponent<MeshFilter>().sharedMesh;
            // Position, rotation and scale of the cube inside the object
            meshes[i].transform = Matrix4x4.TRS(chunks[i].localPosition, chunks[i].localRotation, chunks[i].localScale);
            chunks[i].GetComponent<MeshRenderer>().enabled = false;
        }

        if (combinedMesh != null)
            Destroy(combinedMesh);
        combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(meshes);
        meshFilter.sharedMesh = combinedMesh;
    }

    private void OnDestroy()
    {
        // A mesh created by code is not freed with the object
        if (combinedMesh != null)
            Destroy(combinedMesh);
    }
}
