using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Random = UnityEngine.Random;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    // Only for testing
    private void Update() 
    {
        // Press J to join random room (for testing)
        if (Input.GetKeyDown(KeyCode.J))
        {
            JoinRandomRoom();
        }

        // Press L to leave room (for testing)
        if (Input.GetKeyDown(KeyCode.L))
        {
            LeaveRoom();
        }
    }

    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Connecting to Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
        PhotonNetwork.JoinLobby(); // Join the default lobby
    }

    // Called when joined lobby
    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby!");
    }

    public void CreateRoom(string roomName)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        Debug.Log("Creating room: " + roomName);
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom(string roomName)
    {
        Debug.Log("Attempting to join room: " + roomName);
        PhotonNetwork.JoinRoom(roomName);
    }

    public void JoinRandomRoom()
    {
        Debug.Log("Searching for random room...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Players in room: " + PhotonNetwork.CurrentRoom.PlayerCount + "/2");
        
        // Check if we have 2 players to start the game
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Room is full! Game can start.");
            // TODO: Start the chess game
        }
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No random room available. Creating a new room...");
        CreateRoom("Room_" + Random.Range(1000, 9999));
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player joined: " + newPlayer.NickName);
        
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Room is full! Game can start.");
            // TODO: Start the chess game
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player left: " + otherPlayer.NickName);
        // TODO: Handle player disconnect (opponent left)
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Leaving room...");
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left the room");
    }

    public void Disconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Disconnecting from Photon...");
            PhotonNetwork.Disconnect();
        }
    }
}