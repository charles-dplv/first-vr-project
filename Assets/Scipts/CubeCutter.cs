using System.Collections.Generic;
using UnityEngine;

// Cuts an object into small cubes. In the prefab, the object is made of a few big boxes
// (stretched Unity cubes, children of the object), and each box is replaced by a grid of small cubes
public static class CubeCutter
{
    // Gap between two cubes
    private const float gap = 0.02f;
    // One material per color, shared by all the objects of that color
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    // Need to be called before the Awake of the object: Breakable and XRI read the list of colliders there
    public static void Cut(GameObject obj, float cubeSize, Color color)
    {
        Material material = GetMaterial(color);

        foreach (MeshRenderer box in obj.GetComponentsInChildren<MeshRenderer>(true))
        {
            Vector3 boxSize = box.transform.localScale;
            int countX = CubeCount(boxSize.x, cubeSize);
            int countY = CubeCount(boxSize.y, cubeSize);
            int countZ = CubeCount(boxSize.z, cubeSize);

            // The size of the box is cut evenly, so that the cubes fill it exactly
            Vector3 size = new Vector3(boxSize.x / countX, boxSize.y / countY, boxSize.z / countZ);
            Vector3 corner = box.transform.localPosition - boxSize / 2f;

            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    for (int z = 0; z < countZ; z++)
                    {
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.name = "Chunk";
                        cube.transform.SetParent(obj.transform, false);

                        // Center of the cell, counted from the corner of the box
                        cube.transform.localPosition = corner + new Vector3((x + 0.5f) * size.x, (y + 0.5f) * size.y, (z + 0.5f) * size.z);
                        cube.transform.localScale = size * (1f - gap);
                        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
                    }
                }
            }

            // Not Destroy, which waits for the end of the frame: the room can be shown in this same frame,
            // and the Awake of the object would take the box for a cube
            Object.DestroyImmediate(box.gameObject);
        }
    }

    // At least one cube, for a box thinner than a cube
    private static int CubeCount(float boxSize, float cubeSize)
    {
        return Mathf.Max(1, Mathf.RoundToInt(boxSize / cubeSize));
    }

    private static Material GetMaterial(Color color)
    {
        if (!materials.TryGetValue(color, out Material material))
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            materials.Add(color, material);
        }
        return material;
    }
}
