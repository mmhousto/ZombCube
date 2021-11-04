using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class CreateJoinRooms : MonoBehaviourPunCallbacks
{

    [SerializeField] private string roomName;

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(roomName);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("RoomScene");
    }

    public void ChangeRoomName(string name)
    {
        roomName = name;
    }

    public void LeaveLobby()
    {
        PhotonNetwork.LeaveLobby();
        PhotonNetwork.Disconnect();
        SceneLoader.ToMainMenu();
    }
}
