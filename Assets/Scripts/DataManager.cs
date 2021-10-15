using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;

    public static DataManager Instance { get { return _instance; } }

    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI pointsText;

    /// <summary>
    /// Singleton implementation
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

    }

    // Start is called before the first frame update
    void Start()
    {

        if (coinsText)
        {
            coinsText.text = "Coins: " + Player.Instance.coins;
        }

        if (pointsText)
        {
            pointsText.text = "Points: " + Player.Instance.points;
        }
        
    }

    // Update is called once per frame
    void Update()
    {

        if (coinsText)
        {
            coinsText.text = "Coins: " + Player.Instance.coins;
        }

        if (pointsText)
        {
            pointsText.text = "Points: " + Player.Instance.points;
        }
    }


}
