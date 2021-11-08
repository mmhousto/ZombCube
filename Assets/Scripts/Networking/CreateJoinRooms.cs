using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class CreateJoinRooms : MonoBehaviourPunCallbacks
    {
        [Tooltip("The name of the room the player wants to join or create.")]
        [SerializeField] private string roomName;

        /// <summary>
        /// Creates room with room name and a max number of players set to 4.
        /// </summary>
        public void CreateRoom()
        {
            PhotonNetwork.CreateRoom(roomName, new Photon.Realtime.RoomOptions() { MaxPlayers = 4 });
        }

        /// <summary>
        /// Joins room with room name.
        /// </summary>
        public void JoinRoom()
        {
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// On Successful join of the room, load the RoomScene.
        /// </summary>
        public override void OnJoinedRoom()
        {
            PhotonNetwork.LoadLevel("RoomScene");
        }

        /// <summary>
        /// Dynamic method that Changes room name and updates it in script when player types in input field.
        /// </summary>
        /// <param name="name">The players name they inputted.</param>
        public void ChangeRoomName(string name)
        {
            roomName = name;
        }

        /// <summary>
        /// Leaves the lobby for the player and disconnects them from the Pun network, then loads the main menu.
        /// </summary>
        public void LeaveLobby()
        {
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.Disconnect();
            SceneLoader.ToMainMenu();
        }
    }

}
