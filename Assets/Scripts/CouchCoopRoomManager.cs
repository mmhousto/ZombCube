using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Com.GCTC.ZombCube
{
    public class CouchCoopRoomManager : MonoBehaviour
    {
        int currentPlayers = 0;
        const int MAX_PLAYERS = 4;
        public Button startButton;
        public Button leaveButton;
        public PlayerCard[] playerCards;
        public InputActionReference joinAction;
        public InputActionReference leaveAction;
        public GameObject playerInputPrefab;
        public CouchCoopManager couchCoopManager;

        void Start()
        {
        }

        private void Update()
        {
            if(currentPlayers > 1)
            {
                startButton.interactable = true;
            }
            else
            {
                startButton.interactable = false;
                EventSystem.current.SetSelectedGameObject(leaveButton.gameObject);
            }
        }

        void OnEnable()
        {
            joinAction.action.performed += JoinPlayer;
            leaveAction.action.performed += OnPlayerLeft;
            joinAction.action.Enable();
            leaveAction.action.Enable();

            // Automatically add existing PlayerInput components in the scene
            PlayerInput[] existingPlayers = FindObjectsOfType<PlayerInput>();
            foreach (PlayerInput playerInput in existingPlayers)
            {
                if (currentPlayers == 0) playerInput.neverAutoSwitchControlSchemes = true;
                int playerID = playerInput.devices.FirstOrDefault().device.deviceId;
                if (currentPlayers < MAX_PLAYERS && !couchCoopManager.joinedPlayerIDs.Contains(playerID))
                {
                    playerCards[currentPlayers].UpdateDisplay(currentPlayers + 1);
                    currentPlayers++;

                    // Add the player to the list of joined players
                    couchCoopManager.joinedPlayerIDs.Add(playerID);
                    couchCoopManager.joinedPlayers.Add(playerInput.gameObject);
                    playerInput.actions.FindAction("Cancel").performed += OnPlayerLeft;
                    playerInput.actions.FindAction("Cancel").Enable();
                    DontDestroyOnLoad(playerInput.gameObject);
                }
            }
        }

        void OnDisable()
        {
            joinAction.action.performed -= JoinPlayer;
            leaveAction.action.performed -= OnPlayerLeft;
            joinAction.action.Disable();
            leaveAction.action.Disable();
        }

        public void JoinPlayer(InputAction.CallbackContext context)
        {
            int playerID = context.control.device.deviceId;

            if (currentPlayers < MAX_PLAYERS && !couchCoopManager.joinedPlayerIDs.Contains(playerID))
            {
                playerCards[currentPlayers].UpdateDisplay(currentPlayers+1);
                currentPlayers++;

                GameObject clone = Instantiate(playerInputPrefab);
                DontDestroyOnLoad(clone);

                // Add the player to the list of joined players
                couchCoopManager.joinedPlayerIDs.Add(playerID);
                couchCoopManager.joinedPlayers.Add(clone);
                clone.GetComponent<PlayerInput>().actions.FindAction("Cancel").performed += OnPlayerLeft;
                clone.GetComponent<PlayerInput>().actions.FindAction("Cancel").Enable();
            }
            
        }

        public void Leave()
        {
            if(currentPlayers > 1)
            {
                for (int i = 1; i < couchCoopManager.joinedPlayers.Count; i++)
                {
                    playerCards[i].DisableDisplay();
                    Destroy(couchCoopManager.joinedPlayers[i]);
                }
            }
            
            couchCoopManager.joinedPlayerIDs.Clear();
            couchCoopManager.joinedPlayers.Clear();
            currentPlayers = 0;
        }

        public void OnPlayerLeft(InputAction.CallbackContext context)
        {
            int playerID = context.control.device.deviceId;
            int index = couchCoopManager.joinedPlayerIDs.IndexOf(playerID);

            Debug.Log("Called Player Left");
            if (currentPlayers > 0 && couchCoopManager.joinedPlayerIDs.Contains(playerID) && index != 0 && SceneLoader.GetCurrentScene().name == "MainMenu")
            {
                Debug.Log("Player index: " + index);
                currentPlayers--;
                playerCards[currentPlayers].DisableDisplay();

                GameObject player = couchCoopManager.joinedPlayers[index];
                player.GetComponent<PlayerInput>().actions.FindAction("Cancel").performed -= OnPlayerLeft;
                player.GetComponent<PlayerInput>().actions.FindAction("Cancel").Disable();
                couchCoopManager.joinedPlayers.RemoveAt(index);
                Destroy(player);
                couchCoopManager.joinedPlayerIDs.Remove(playerID);
            }


        }
    }
}
