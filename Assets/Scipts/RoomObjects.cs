using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// The objects of the rage room, all made of cubes with ChunkBuilder.
// Each shape is a lambda that says if the cell (x, y, z) gets a cube. Sizes are in cubes
public static class RoomObjects
{
    // Side of a cube (m). Bigger cubes = fewer objects for the Quest to handle
    public const float FurnitureCube = 0.12f;
    public const float ItemCube = 0.06f;

    // --- Furniture: heavy, breaks around the impact ---

    // Top layer, and one leg in each corner
    public static GameObject Table(int width, int height, int depth, Color color)
    {
        GameObject table = ChunkBuilder.Build("Table", new Vector3Int(width, height, depth), FurnitureCube, color,
            (x, y, z) => y == height - 1 || (IsBorder(x, width) && IsBorder(z, depth)));
        return MakeFurniture(table);
    }

    // Two sides, and a board every levelHeight cubes (the top one included)
    public static GameObject Shelf(int width, int levels, int levelHeight, int depth, Color color)
    {
        GameObject shelf = ChunkBuilder.Build("Shelf", new Vector3Int(width, levels * levelHeight + 1, depth), FurnitureCube, color,
            (x, y, z) => IsBorder(x, width) || y % levelHeight == 0);
        return MakeFurniture(shelf);
    }

    // --- Objects you can grab and throw: they shatter completely ---

    // Seat, backrest on one side, and four legs
    public static GameObject Chair(int width, int seatHeight, int backHeight, Color color)
    {
        GameObject chair = ChunkBuilder.Build("Chair", new Vector3Int(width, seatHeight + 1 + backHeight, width), FurnitureCube, color,
            (x, y, z) => y == seatHeight
                || (y > seatHeight && z == 0)
                || (y < seatHeight && IsBorder(x, width) && IsBorder(z, width)));
        return MakeGrabbable(chair, 4f, 1f, 0.08f);
    }

    // Hollow box: only the cells on a face
    public static GameObject Crate(int size, Color color)
    {
        GameObject crate = ChunkBuilder.Build("Crate", new Vector3Int(size, size, size), FurnitureCube, color,
            (x, y, z) => IsBorder(x, size) || IsBorder(y, size) || IsBorder(z, size));
        return MakeGrabbable(crate, 4f, 1f, 0.08f);
    }

    // Round shape whose radius follows a sine curve: narrow at the foot and the neck, wide in the middle
    public static GameObject Vase(int width, int height, Color color)
    {
        const float neck = 0.8f;
        float belly = width / 2f;
        GameObject vase = ChunkBuilder.Build("Vase", new Vector3Int(width, height, width), ItemCube, color,
            (x, y, z) => DistanceToAxis(x, z, width) <= neck + (belly - neck) * Mathf.Sin(Mathf.PI * (y + 0.5f) / height));
        return MakeGrabbable(vase, 2.5f, 0.3f, 0.012f);
    }

    // Body 3 cubes wide, neck of 1 cube on the last two layers
    public static GameObject Bottle(int height, Color color)
    {
        GameObject bottle = ChunkBuilder.Build("Bottle", new Vector3Int(3, height, 3), ItemCube, color,
            (x, y, z) => y < height - 2 || (x == 1 && z == 1));
        return MakeGrabbable(bottle, 2.5f, 0.3f, 0.02f);
    }

    // Flat disc, one cube thick
    public static GameObject Plate(int width, Color color)
    {
        GameObject plate = ChunkBuilder.Build("Plate", new Vector3Int(width, 1, width), ItemCube, color,
            (x, y, z) => DistanceToAxis(x, z, width) <= width / 2f);
        return MakeGrabbable(plate, 2.5f, 0.3f, 0.02f);
    }

    private static bool IsBorder(int i, int count)
    {
        return i == 0 || i == count - 1;
    }

    // Distance (in cubes) between the cell and the vertical axis in the middle of a square grid
    private static float DistanceToAxis(int x, int z, int width)
    {
        float center = (width - 1) / 2f;
        return new Vector2(x - center, z - center).magnitude;
    }

    private static GameObject MakeFurniture(GameObject obj)
    {
        obj.AddComponent<Breakable>().Setup(3.5f, 3f, 0.25f, 0.2f);
        return obj;
    }

    private static GameObject MakeGrabbable(GameObject obj, float minSpeed, float minImpulse, float chunkMass)
    {
        // The break radius does not matter: a grabbable object shatters completely (see Breakable)
        obj.AddComponent<Breakable>().Setup(minSpeed, minImpulse, 0f, chunkMass);

        Rigidbody body = obj.GetComponent<Rigidbody>();
        // Smooth movement in the hand, and no going through thin objects when thrown fast
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        XRGrabInteractable grab = obj.AddComponent<XRGrabInteractable>();
        // Velocity tracking: the held object stays a non-kinematic body, so it still hits walls and objects
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        // Grabbed where the hand touches it, instead of snapping its pivot into the hand
        grab.useDynamicAttach = true;
        return obj;
    }
}
