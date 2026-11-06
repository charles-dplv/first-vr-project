using UnityEngine;

// Fills a room with prefabs cut into small cubes. The same id always gives the same room
public class RoomGenerator : MonoBehaviour
{
    [SerializeField] private int seed = 0;
    [SerializeField] private Vector3 zoneCenter = new Vector3(-1.85f, 0f, 1.85f);
    [SerializeField] private Vector2 zoneSize = new Vector2(6.2f, 4.2f);

    // Minimum distance between two pieces of furniture (m)
    [SerializeField] private float minDistance = 1.8f;
    // No new piece of furniture above this number of cubes: too many of them is too slow on Quest
    [SerializeField] private int maxCubes = 1100;

    // Tables and shelves: small objects are put on their "Slot" children
    [SerializeField] private GameObject[] furniturePrefabs;
    // Their "Slot" child is the place of the next crate of the stack
    [SerializeField] private GameObject[] cratePrefabs;
    [SerializeField] private GameObject chairPrefab;
    [SerializeField] private GameObject[] itemPrefabs;

    // Side of a cube (m). Bigger cubes = fewer objects for the Quest to handle
    private const float furnitureCube = 0.12f;
    private const float itemCube = 0.06f;

    private static readonly Color wood = new Color(0.55f, 0.4f, 0.25f);

    public void Generate(Transform room, int id)
    {
        Random.InitState(seed + id);

        // Colors of the room: one hue for the furniture, the opposite hue for the small objects
        float hue = Random.Range(0, 12) / 12f;
        Color furnitureColor = Color.HSVToRGB(hue, 0.35f, 0.7f);
        Color itemColor = Color.HSVToRGB(Mathf.Repeat(hue + 0.5f, 1f), 0.7f, 0.9f);

        // One spot per piece of furniture, never too close to another one
        foreach (Vector2 spot in PoissonDisk.Sample(zoneSize, minDistance))
        {
            if (room.GetComponentsInChildren<Collider>(true).Length >= maxCubes)
                break;

            Vector3 position = zoneCenter + new Vector3(spot.x - zoneSize.x / 2f, 0f, spot.y - zoneSize.y / 2f);
            // Mostly aligned with the walls, but a bit messy
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f + Random.Range(-15f, 15f), 0f);

            float type = Random.value;
            if (type < 0.6f)
                AddFurniture(room, position, rotation, furnitureColor, itemColor);
            else if (type < 0.85f)
                AddCrates(room, position, rotation);
            else
                AddChairs(room, position, rotation, furnitureColor);
        }
    }

    // A table or a shelf, with a small object on 40% of its slots
    private void AddFurniture(Transform room, Vector3 position, Quaternion rotation, Color color, Color itemColor)
    {
        GameObject furniture = Spawn(RandomPrefab(furniturePrefabs), room, position, rotation, furnitureCube, color);

        foreach (Transform child in furniture.transform)
        {
            if (child.name == "Slot" && Random.value < 0.4f)
            {
                Vector3 itemPosition = position + rotation * child.localPosition;
                Spawn(RandomPrefab(itemPrefabs), room, itemPosition, rotation, itemCube, itemColor);
            }
        }
    }

    // A stack of 1 to 3 crates, each one a bit shifted and turned.
    // All the same size: a big crate on a small one is unstable and can fall by itself
    private void AddCrates(Transform room, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = RandomPrefab(cratePrefabs);
        int count = Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            Vector3 shift = new Vector3(Random.Range(-0.05f, 0.05f), 0f, Random.Range(-0.05f, 0.05f));
            Quaternion turn = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
            GameObject crate = Spawn(prefab, room, position + rotation * shift, rotation * turn, furnitureCube, wood);
            position.y += crate.transform.Find("Slot").localPosition.y;
        }
    }

    // One or two chairs side by side
    private void AddChairs(Transform room, Vector3 position, Quaternion rotation, Color color)
    {
        int count = Random.Range(1, 3);
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * 0.7f;
            Spawn(chairPrefab, room, position + rotation * new Vector3(x, 0f, 0f), rotation, furnitureCube, color);
        }
    }

    private GameObject RandomPrefab(GameObject[] prefabs)
    {
        return prefabs[Random.Range(0, prefabs.Length)];
    }

    // The room is inactive while it is built, so the Awake of the new object waits until the room is shown:
    // Breakable and XRI will see the small cubes, not the big boxes of the prefab
    private GameObject Spawn(GameObject prefab, Transform room, Vector3 position, Quaternion rotation, float cubeSize, Color color)
    {
        GameObject obj = Instantiate(prefab, room);
        obj.transform.SetLocalPositionAndRotation(position, rotation);
        CubeCutter.Cut(obj, cubeSize, color);
        return obj;
    }
}
