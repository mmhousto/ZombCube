using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class CouchCoopRoomManager : MonoBehaviour
    {
        int currentPlayers = 0;
        const int MAX_PLAYERS = 4;
        public PlayerCard[] playerCards;
        public InputActionReference joinAction;
        public GameObject playerInputPrefab;
        public CouchCoopManager couchCoopManager;

        void Start()
        {
            // Automatically add existing PlayerInput components in the scene
            PlayerInput[] existingPlayers = FindObjectsOfType<PlayerInput>();
            foreach (PlayerInput playerInput in existingPlayers)
            {
                int playerID = playerInput.devices.FirstOrDefault().device.deviceId;
                if (currentPlayers < MAX_PLAYERS && !couchCoopManager.joinedPlayerIDs.Contains(playerID))
                {
                    playerCards[currentPlayers].UpdateDisplay(currentPlayers + 1);
                    currentPlayers++;
                    
                    // Add the player to the list of joined players
                    couchCoopManager.joinedPlayerIDs.Add(playerID);
                    //couchCoopManager.joinedPlayers.Add(playerInput.gameObject);
                }
            }
        }

        void OnEnable()
        {
            joinAction.action.performed += JoinPlayer;
            joinAction.action.Enable();
        }

        void OnDisable()
        {
            joinAction.action.performed -= JoinPlayer;
            joinAction.action.Disable();
        }

        public void JoinPlayer(InputAction.CallbackContext context)
        {
            int playerID = context.control.device.deviceId;

            if (currentPlayers < MAX_PLAYERS && !couchCoopManager.joinedPlayerIDs.Contains(playerID))
            {
                playerCards[currentPlayers].UpdateDisplay(currentPlayers+1);
                currentPlayers++;

                //GameObject clone = Instantiate(playerInputPrefab);
                //DontDestroyOnLoad(clone);

                // Add the player to the list of joined players
                couchCoopManager.joinedPlayerIDs.Add(playerID);
                //couchCoopManager.joinedPlayers.Add(clone);
            }
            
        }

        public void Leave()
        {
            foreach(PlayerCard card in playerCards)
            {
                card.DisableDisplay();
            }
            currentPlayers = 0;
        }

        public void OnPlayerLeft()
        {
            if(currentPlayers > 0)
            {
                currentPlayers--;
                playerCards[currentPlayers].DisableDisplay();
                //Destroy(couchCoopManager.joinedPlayers[currentPlayers]);
            }


        }
    }
}
