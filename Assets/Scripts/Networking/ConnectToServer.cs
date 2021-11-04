using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    private void OnConnectedToServer()
    {
        
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log(cause);
        SceneLoader.ToMainMenu();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();

    }

    public override void OnJoinedLobby()
    {
        SceneLoader.ToLobby();
        PhotonNetwork.NickName = Player.Instance.playerName;
        Debug.Log(PhotonNetwork.NickName);
    }
}
