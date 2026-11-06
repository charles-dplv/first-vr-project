using System;
using System.Collections.Generic;
using UnityEngine;

// Builds an object out of small cubes, like voxels: the object is a grid of cells,
// and the shape (isFull) says which cells get a cube
public static class ChunkBuilder
{
    // Gap between two cubes
    private const float gap = 0.02f;
    // One material per color, shared by all the objects of that color
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    public static GameObject Build(string name, Vector3Int size, float cubeSize, Color color, Func<int, int, int, bool> isFull)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        Material material = GetMaterial(color);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if (!isFull(x, y, z))
                        continue;

                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Chunk_{x}_{y}_{z}";
                    cube.transform.SetParent(obj.transform, false);

                    // Center of the cell from the bottom center of the grid
                    cube.transform.localPosition = new Vector3(x - (size.x - 1) / 2f, y + 0.5f, z - (size.z - 1) / 2f) * cubeSize;
                    cube.transform.localScale = Vector3.one * cubeSize * (1f - gap);
                    cube.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
            }
        }
        return obj;
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
