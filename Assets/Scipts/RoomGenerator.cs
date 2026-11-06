using UnityEngine;

// Fills a room with furniture and small objects. The same id always gives the same room
public class RoomGenerator : MonoBehaviour
{
    [SerializeField] private int seed = 0;
    // Area where the furniture goes (yellow gizmo). It is out of sight from the detector, and it leaves free
    // a path along the inner walls, so that the player can always walk the loop
    [SerializeField] private Vector3 zoneCenter = new Vector3(-1.85f, 0f, 1.85f);
    [SerializeField] private Vector2 zoneSize = new Vector2(6.2f, 4.2f);
    // Minimum distance between two pieces of furniture (m)
    [SerializeField] private float minDistance = 1.8f;

    private static readonly Color wood = new Color(0.55f, 0.4f, 0.25f);

    public void Generate(Transform room, int id)
    {
        // Same seed = same random numbers = same room
        Random.InitState(seed + id);

        // Colors of the room: one hue for the furniture, the opposite hue for the small objects
        float hue = Random.Range(0, 12) / 12f;
        Color furnitureColor = Color.HSVToRGB(hue, 0.35f, 0.7f);
        Color itemColor = Color.HSVToRGB(Mathf.Repeat(hue + 0.5f, 1f), 0.7f, 0.9f);

        // One spot per piece of furniture, never too close to another one
        foreach (Vector2 spot in PoissonDisk.Sample(zoneSize, minDistance))
        {
            Vector3 position = zoneCenter + new Vector3(spot.x - zoneSize.x / 2f, 0f, spot.y - zoneSize.y / 2f);
            // Mostly aligned with the walls, but a bit messy
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f + Random.Range(-15f, 15f), 0f);

            float type = Random.value;
            if (type < 0.4f)
                AddTable(room, position, rotation, furnitureColor, itemColor);
            else if (type < 0.6f)
                AddShelf(room, position, rotation, furnitureColor, itemColor);
            else if (type < 0.85f)
                AddCrates(room, position, rotation);
            else
                AddChairs(room, position, rotation, furnitureColor);
        }
    }

    private void AddTable(Transform room, Vector3 position, Quaternion rotation, Color color, Color itemColor)
    {
        int width = Random.Range(8, 12);
        int height = 6;
        int depth = Random.Range(5, 8);
        Place(RoomObjects.Table(width, height, depth, color), room, position, rotation);

        // Slots of at least 40 cm on the top, so that the objects do not overlap
        float length = width * RoomObjects.FurnitureCube;
        int count = Random.Range(1, Mathf.FloorToInt(length / 0.4f) + 1);
        AddItems(room, position, rotation, height * RoomObjects.FurnitureCube, length, count, itemColor);
    }

    private void AddShelf(Transform room, Vector3 position, Quaternion rotation, Color color, Color itemColor)
    {
        int width = Random.Range(7, 10);
        int levels = 3;
        int levelHeight = 4;
        Place(RoomObjects.Shelf(width, levels, levelHeight, 3, color), room, position, rotation);

        // 0 to 2 objects on each board, between the two sides
        float length = (width - 2) * RoomObjects.FurnitureCube;
        for (int level = 0; level <= levels; level++)
        {
            float height = (level * levelHeight + 1) * RoomObjects.FurnitureCube;
            AddItems(room, position, rotation, height, length, Random.Range(0, 3), itemColor);
        }
    }

    // A stack of 1 to 3 crates, each one a bit shifted and turned
    private void AddCrates(Transform room, Vector3 position, Quaternion rotation)
    {
        float height = 0f;
        int count = Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            int size = Random.Range(3, 5);
            Vector3 shift = new Vector3(Random.Range(-0.05f, 0.05f), height, Random.Range(-0.05f, 0.05f));
            Quaternion turn = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
            Place(RoomObjects.Crate(size, wood), room, position + rotation * shift, rotation * turn);
            // Put the next crate 1 cm above this one, so that they do not overlap
            height += size * RoomObjects.FurnitureCube + 0.01f;
        }
    }

    // One or two chairs side by side
    private void AddChairs(Transform room, Vector3 position, Quaternion rotation, Color color)
    {
        int count = Random.Range(1, 3);
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * 0.7f;
            Place(RoomObjects.Chair(4, 3, 4, color), room, position + rotation * new Vector3(x, 0f, 0f), rotation);
        }
    }

    // Puts count small objects side by side on a surface: the surface is cut into count slots, one object in the middle of each
    private void AddItems(Transform room, Vector3 position, Quaternion rotation, float height, float length, int count, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            float x = ((i + 0.5f) / count - 0.5f) * length;
            // 1 cm above the surface: an object that starts inside another one would be pushed out hard and break
            Vector3 itemPosition = position + rotation * new Vector3(x, height + 0.01f, 0f);
            Place(RandomItem(color), room, itemPosition, rotation);
        }
    }

    private GameObject RandomItem(Color color)
    {
        switch (Random.Range(0, 3))
        {
            case 0:
                return RoomObjects.Vase(Random.Range(4, 6), Random.Range(4, 6), color);
            case 1:
                return RoomObjects.Bottle(5, color);
            default:
                return RoomObjects.Plate(5, color);
        }
    }

    private void Place(GameObject obj, Transform room, Vector3 position, Quaternion rotation)
    {
        obj.transform.SetParent(room, false);
        obj.transform.SetLocalPositionAndRotation(position, rotation);
        // The room is inactive while it is built: Awake of the object will run when the room is shown
        obj.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        // Rooms are children of RoomsParent, which is at the origin
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(zoneCenter, new Vector3(zoneSize.x, 0.05f, zoneSize.y));
    }
}
