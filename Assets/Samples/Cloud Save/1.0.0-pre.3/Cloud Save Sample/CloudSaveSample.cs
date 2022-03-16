using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace CloudSaveSample
{

    public class CloudSaveSample : MonoBehaviour
    {
        private static CloudSaveSample instance;

        public static CloudSaveSample Instance { get { return instance; } }

        Com.MorganHouston.ZombCube.SaveData data;

        public Com.MorganHouston.ZombCube.Player player;

        public string playerID;

        private async void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this;
            }
            DontDestroyOnLoad(this.gameObject);

            // Authenticate Unity Services.
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                Debug.Log("Services are already Initialized");
            }
            else
                await UnityServices.InitializeAsync();

        }







        /// <summary>
        /// Sign in anonymously
        /// </summary>
        public async void SignIn()
        {
            // Cloud Save needs to be initialized along with the other Unity Services that
            // it depends on (namely, Authentication), and then the user must sign in.
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                Debug.Log("Services are already Initialized");
            }
            else
                await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Player is signed in");
            }
            else
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            playerID = AuthenticationService.Instance.PlayerId;
            HashSet<string> hashSet = new HashSet<string> { playerID };
            var results = SaveData.LoadAsync(hashSet);
            if (results != null)
            {
                try
                {
                    Com.MorganHouston.ZombCube.SaveData incomingSample = await RetrieveSpecificData<Com.MorganHouston.ZombCube.SaveData>(playerID);
                    Debug.Log($"Loaded object: {incomingSample.playerName}, {incomingSample.points}, {incomingSample.currentBlaster}");
                    LoadPlayerData(incomingSample);
                }
                catch (Exception e)
                {
                    Debug.Log(e + "\nNo profile to download from the cloud! Try local profile.");
                    try
                    {
                        LoadPlayerData();
                    }
                    catch (Exception e2)
                    {
                        Debug.Log($"Error: {e2}\nNew Player");
                        return;
                    }
                }

            }
            else
            {
                Debug.Log("No profile to download from the cloud! Try to download local profile.");
                try
                {
                    LoadPlayerData();
                }
                catch (Exception e)
                {
                    Debug.Log($"Error: {e}\nNew Player");
                    return;
                }
            }
            Com.MorganHouston.ZombCube.SceneLoader.ToMainMenu();
        }

        private async Task ListAllKeys()
        {
            try
            {
                var keys = await SaveData.RetrieveAllKeysAsync();

                Debug.Log($"Keys count: {keys.Count}\n" +
                          $"Keys: {String.Join(", ", keys)}");
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        private async Task ForceSaveSingleData(string key, string value)
        {
            try
            {
                Dictionary<string, object> oneElement = new Dictionary<string, object>();

                // It's a text input field, but let's see if you actually entered a number.
                if (Int32.TryParse(value, out int wholeNumber))
                {
                    oneElement.Add(key, wholeNumber);
                }
                else if (Single.TryParse(value, out float fractionalNumber))
                {
                    oneElement.Add(key, fractionalNumber);
                }
                else
                {
                    oneElement.Add(key, value);
                }

                await SaveData.ForceSaveAsync(oneElement);

                Debug.Log($"Successfully saved {key}:{value}");
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        public async Task SavePlayerData(Com.MorganHouston.ZombCube.SaveData data)
        {
            await ForceSaveObjectData(playerID, data);
        }

        private async Task ForceSaveObjectData(string key, object value)
        {
            try
            {
                // Although we are only saving a single value here, you can save multiple keys
                // and values in a single batch.
                Dictionary<string, object> oneElement = new Dictionary<string, object>
                {
                    { key, value }
                };

                await SaveData.ForceSaveAsync(oneElement);

                Debug.Log($"Successfully saved {key}:{value}");
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        private async Task<T> RetrieveSpecificData<T>(string key)
        {
            try
            {
                var results = await SaveData.LoadAsync(new HashSet<string> { key });

                if (results.TryGetValue(key, out string value))
                {
                    return JsonUtility.FromJson<T>(value);
                }
                else
                {
                    Debug.Log($"There is no such key as {key}!");
                }
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }

            return default;
        }

        private async Task RetrieveEverything()
        {
            try
            {
                // If you wish to load only a subset of keys rather than everything, you
                // can call a method LoadAsync and pass a HashSet of keys into it.
                var results = await SaveData.LoadAllAsync();

                Debug.Log($"Elements loaded!");

                foreach (var element in results)
                {
                    Debug.Log($"Key: {element.Key}, Value: {element.Value}");
                }
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        private async Task ForceDeleteSpecificData(string key)
        {
            try
            {
                await SaveData.ForceDeleteAsync(key);

                Debug.Log($"Successfully deleted {key}");
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }







        /// <summary>
        /// Loads the players data and sets it to the player.
        /// </summary>
        public void LoadPlayerData()
        {
            Com.MorganHouston.ZombCube.SaveData data = Com.MorganHouston.ZombCube.SaveSystem.LoadPlayer();

            player.playerName = data.playerName;
            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;
            player.currentBlaster = data.currentBlaster;
            player.currentSkin = data.currentSkin;
            player.ownedBlasters = data.ownedBlasters;
            player.ownedSkins = data.ownedSkins;
        }

        public void LoadPlayerData(Com.MorganHouston.ZombCube.SaveData data)
        {

            player.playerName = data.playerName;
            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;

            player.currentBlaster = data.currentBlaster;
            if (data.ownedBlasters == null)
            {
                player.ownedBlasters = new int[] { 1, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (data.ownedBlasters.Length != 8)
            {
                int[] temp = new int[8];
                data.ownedBlasters.CopyTo(temp, 0);
                data.ownedBlasters = temp;
                player.ownedBlasters = data.ownedBlasters;
            }

            player.currentSkin = data.currentSkin;

            if (data.ownedSkins == null)
            {
                player.ownedSkins = new int[] { 1, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (data.ownedSkins.Length != 8)
            {
                int[] temp = new int[8];
                data.ownedBlasters.CopyTo(temp, 0);
                data.ownedBlasters = temp;
                player.ownedBlasters = data.ownedBlasters;

            }
            else
            {
                player.ownedSkins = data.ownedSkins;
            }
        }
    }
}