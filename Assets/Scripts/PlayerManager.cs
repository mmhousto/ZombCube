using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour, IDamageable<float>
{
    private static PlayerManager _instance;

    public static PlayerManager Instance { get { return _instance; } }

    public Material[] blasterMaterial;

    public int currentPoints = 0;
    public TextMeshProUGUI scoreText;
    public Slider healthBar;

    private float healthPoints = 100f;
    private bool isGameOver;
    private int currentBlaster = 0;

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        currentBlaster = DataManager.Instance.GetBlaster();
    }

    // Start is called before the first frame update
    void Start()
    {
        currentPoints = 0;
        healthBar.value = healthPoints;
        scoreText.text = "Score: " + currentPoints.ToString();

        GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

        foreach(GameObject item in blaster)
        {
            item.GetComponent<MeshRenderer>().material = blasterMaterial[currentBlaster];
        }
    }

    // Update is called once per frame
    void Update()
    {
        healthBar.value = healthPoints;
        scoreText.text = "Score: " + currentPoints.ToString();

        if(healthPoints <= 0 && !isGameOver)
        {
            healthPoints = 0;
            UpdateTotalPoints();
            GameManager.Instance.GameOver();
            isGameOver = true;
        }
    }

    public void AddPoints(int pointsToAdd)
    {
        currentPoints += pointsToAdd;
    }

    public void Damage(float damageTaken)
    {
        healthPoints -= damageTaken;
    }

    public void UpdateTotalPoints()
    {
        PlayerPrefs.SetInt("Points", DataManager.Instance.GetPoints() + currentPoints);
    }
}
