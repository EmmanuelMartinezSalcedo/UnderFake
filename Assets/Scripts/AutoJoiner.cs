using UnityEngine;
using Alteruna;
using Alteruna.Trinity;
using UnityEngine.SceneManagement;

public class AutoJoinRoom : MonoBehaviour
{
    public string RoomName = "global";
    public ushort MaxUsers = 10;

    private Multiplayer _multiplayer;
    private bool _attemptedJoin = false;

    public GameObject newObjectPrefab;
    public Vector3 spawnPosition;

    private void Start()
    {
        _multiplayer = FindFirstObjectByType<Multiplayer>();

        if (_multiplayer == null)
        {
            Debug.LogError("No Multiplayer component found in scene.");
            return;
        }

        _multiplayer.OnConnected.AddListener(OnConnected);
        _multiplayer.OnRoomListUpdated.AddListener(OnRoomListUpdated);

        if (_multiplayer.IsConnected)
        {
            OnConnected(_multiplayer, null);
        }
    }

    private void OnConnected(Multiplayer multiplayer, Endpoint endpoint)
    {
        _multiplayer.RefreshRoomList();
    }

    private void OnRoomListUpdated(Multiplayer multiplayer)
    {
        if (_attemptedJoin) return;
        _attemptedJoin = true;

        foreach (var room in multiplayer.AvailableRooms)
        {
            if (room.Name.ToLower() == RoomName.ToLower())
            {
                Debug.Log("Joining existing room: " + RoomName);
                room.Join();
                GameObject newObj = Instantiate(newObjectPrefab, spawnPosition, Quaternion.identity);
                return;
            }
        }

        Debug.Log("Creating room: " + RoomName);
        _multiplayer.CreateRoom(RoomName,false,0,true,true,MaxUsers);
    }
}
