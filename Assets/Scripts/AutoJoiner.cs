using UnityEngine;
using Alteruna;

public class AutoJoinRoom : MonoBehaviour
{
    public string RoomName = "global";
    public ushort MaxUsers = 10;

    private Multiplayer _multiplayer;
    private bool _attemptedJoin = false;

    public GameObject enemiesSpawner;
    public GameObject arrowSpawner;
    public Vector3 spawnPosition;

    public GameObject player1;
    public GameObject player2;

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
                GameObject newObj = Instantiate(enemiesSpawner, spawnPosition, Quaternion.identity);
                //GameObject newObj2 = Instantiate(arrowSpawner, spawnPosition, Quaternion.identity);
                Debug.LogWarning("enemiesSpawner creado exitosamente.");
                return;
            }
        }

        Debug.Log("Creating room: " + RoomName);
        _multiplayer.CreateRoom(RoomName,false,0,true,true,MaxUsers);
    }
}
