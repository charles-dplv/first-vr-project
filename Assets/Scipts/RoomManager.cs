using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private GameObject roomsParent;
    private Dictionary<int, GameObject> rooms = new Dictionary<int, GameObject>();
    private int currentRoomId = 1000;

    private void Awake()
    {
        CreateRoom(currentRoomId);
        rooms[currentRoomId].SetActive(true);
    }

    private void CreateRoom(int id)
    {
        GameObject room = new GameObject("Room_" + id);
        room.SetActive(false);
        
        // Add a random (sphere or cube) object to the room's content
        GameObject objectToAdd;
        if (Random.value > 0.5f)
        {
            objectToAdd = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        else
        {
            objectToAdd = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        objectToAdd.transform.SetParent(room.transform, false);
        rooms.Add(id, room);
        room.transform.SetParent(roomsParent.transform, false);
    }

    public void ChangeRoom(int direction)
    {
        int previousRoomId = currentRoomId;
        currentRoomId += direction;

        // Create the room if it doesn't exist yet, then show it
        if (!rooms.ContainsKey(currentRoomId))
        {
            CreateRoom(currentRoomId);
        }
        rooms[currentRoomId].SetActive(true);

        // Deactivate all other rooms
        rooms[previousRoomId].SetActive(false);

        Debug.Log("Current Room ID: " + currentRoomId);
    }
}
