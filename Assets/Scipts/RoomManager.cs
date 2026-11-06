using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private GameObject roomsParent;
    [SerializeField] private RoomGenerator generator;
    private Dictionary<int, GameObject> rooms = new Dictionary<int, GameObject>();
    private int currentRoomId = 1000;

    public Transform CurrentRoom => rooms[currentRoomId].transform;

    private void Awake()
    {
        CreateRoom(currentRoomId);
        rooms[currentRoomId].SetActive(true);
    }

    private void CreateRoom(int id)
    {
        GameObject room = new GameObject("Room_" + id);
        room.SetActive(false);
        room.transform.SetParent(roomsParent.transform, false);
        generator.Generate(room.transform, id);

        // An object carried through the detector belongs to the room where it is dropped.
        // Otherwise XRI puts it back in its first room, or leaves it out of every room if that one is hidden
        foreach (XRGrabInteractable grab in room.GetComponentsInChildren<XRGrabInteractable>(true))
        {
            grab.retainTransformParent = false;
            grab.selectExited.AddListener(OnObjectDropped);
        }

        rooms.Add(id, room);
        Debug.Log("Room " + id + ": " + room.GetComponentsInChildren<Collider>(true).Length + " cubes");
    }

    private void OnObjectDropped(SelectExitEventArgs args)
    {
        // Canceled when the object is destroyed in the hand (it shattered)
        if (!args.isCanceled)
            args.interactableObject.transform.SetParent(CurrentRoom, true);
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
