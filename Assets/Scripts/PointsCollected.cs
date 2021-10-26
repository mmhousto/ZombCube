using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PointsCollected : MonoBehaviour
{

    private TextMeshProUGUI pointsCollected;

    // Start is called before the first frame update
    void Start()
    {
        pointsCollected = GetComponent<TextMeshProUGUI>();
        if(GameManager.Instance)
            pointsCollected.text = "Score: " + GameManager.Instance.currentPoints.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance)
            pointsCollected.text = "Score: " + GameManager.Instance.currentPoints.ToString();
    }
}
