using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

namespace Com.GCTC.ZombCube
{

    public class StatsManager : MonoBehaviour
    {

        public TextMeshProUGUI userIdLabel, userNameLabel, playerNameLabel, cubesDestroyedLabel,
            currentBlasterLabel, currentSkinLabel, soloWaveLabel, partyWaveLabel, projectilesLabel, totalPointsLabel;

        // Start is called before the first frame update
        void Start()
        {
        }

        private void OnEnable()
        {
            SetLabels();
        }

        private void SetLabels()
        {
            userIdLabel.text = Player.Instance.userID.Substring(0, 25 > Player.Instance.userID.Length ? Player.Instance.userID.Length : 25);
            userNameLabel.text = Player.Instance.userName.Substring(0, 25 > Player.Instance.userName.Length ? Player.Instance.userName.Length : 25);
            playerNameLabel.text = Player.Instance.playerName.Substring(0, 25>Player.Instance.playerName.Length?Player.Instance.playerName.Length:25);
            cubesDestroyedLabel.text = $"{Player.Instance.cubesEliminated}";

            currentBlasterLabel.text = $"{GetColor(Player.Instance.currentBlaster)}";
            currentSkinLabel.text = $"{GetColor(Player.Instance.currentSkin)}";

            soloWaveLabel.text = $"{Player.Instance.highestWave}";
            partyWaveLabel.text = $"{Player.Instance.highestWaveParty}";
            projectilesLabel.text = $"{Player.Instance.totalProjectilesFired}";
            totalPointsLabel.text = $"{Player.Instance.totalPointsEarned}";
        }

        private string GetColor(int index)
        {
            string result = "";
            switch (index)
            {
                case 0:
                    result = "White";
                    break;
                case 1:
                    result = "Green";
                    break;
                case 2:
                    result = "Blue";
                    break;
                case 3:
                    result = "Yellow";
                    break;
                case 4:
                    result = "Pink";
                    break;
                case 5:
                    result = "Legend";
                    break;
                case 6:
                    result = "OSU";
                    break;
                case 7:
                    result = "OU";
                    break;

            }
            return result;
        }
    }

}