using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{

    public class PhotonPlayer : MonoBehaviour, IPunInstantiateMagicCallback
    {

        private PhotonView PV;
        private PlayerInput playerInput;
        int numOfPlayers;
        GameObject myPlayer;

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            info.Sender.TagObject = myPlayer;
        }

        // Start is called before the first frame update
        void Start()
        {
            numOfPlayers = NetworkGameManager.Instance.playersSpawned;
            PV = GetComponent<PhotonView>();
            playerInput = GetComponent<PlayerInput>();
            playerInput.enabled = false;

            if (PV.IsMine)
            {
                switch (numOfPlayers)
                {
                    case 0:
                        myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                            GameSetup.GS.spawnLocations[0].position,
                            GameSetup.GS.spawnLocations[0].rotation, 0) as GameObject;
                        NetworkGameManager.Instance.CallPlayerSpawned();
                        break;
                    case 1:
                        myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                            GameSetup.GS.spawnLocations[1].position,
                            GameSetup.GS.spawnLocations[1].rotation, 0) as GameObject;
                        NetworkGameManager.Instance.CallPlayerSpawned();
                        break;
                    case 2:
                        myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                            GameSetup.GS.spawnLocations[2].position,
                            GameSetup.GS.spawnLocations[2].rotation, 0) as GameObject;
                        NetworkGameManager.Instance.CallPlayerSpawned();
                        break;
                    case 3:
                        myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                            GameSetup.GS.spawnLocations[3].position,
                            GameSetup.GS.spawnLocations[3].rotation, 0) as GameObject;
                        NetworkGameManager.Instance.CallPlayerSpawned();
                        break;
                }
            }
        }

        void Update()
        {
            if(PV.IsMine && NetworkSpectatorManager.isAlive == false && playerInput.enabled == false)
            {
                playerInput.enabled = true;
            }
        }

        public void OnNextPlayer(InputValue value)
        {
            if (value.isPressed && NetworkGameManager.Instance.IsGameOver() == false)
            {
                NetworkSpectatorManager.ShowNextPlayerCam();
            }
        }

    }
}
