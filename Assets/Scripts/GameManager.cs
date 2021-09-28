using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{

    private static int CurrentRound { get; set; }
    public TextMeshProUGUI waveTxt;

    // Start is called before the first frame update
    void Start()
    {
        CurrentRound = 1;
        waveTxt.text = "Wave: " + CurrentRound.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        waveTxt.text = "Wave: " + CurrentRound.ToString();
    }

    public void NextWave()
    {
        CurrentRound += 1;
    }
}
