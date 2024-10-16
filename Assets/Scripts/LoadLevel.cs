using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Com.GCTC.ZombCube;

public class LoadLevel : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI loadingText;

    private int dots = 1;

    void Start()
    {
        InvokeRepeating(nameof(UpdateLoadingText), 0.0f, 1f);
        StartCoroutine(LoadAsynchronously(SceneLoader.levelToLoad));
    }

    IEnumerator LoadAsynchronously(int index)
    {
#if UNITY_WSA
        LightmapSettings.lightmaps = new LightmapData[0];
        Resources.UnloadUnusedAssets();
#endif

        AsyncOperation operation = SceneManager.LoadSceneAsync(index);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / .9f);

            slider.value = progress;
            progressText.text = progress * 100f + "%";

            yield return null;
        }
    }

    void UpdateLoadingText()
    {
        dots++;
        if(dots > 3)
        {
            dots = 1;
        }

        switch (dots)
        {
            case 1:
                loadingText.text = "Loading.";
                break;
            case 2:
                loadingText.text = "Loading..";
                break;
            case 3:
                loadingText.text = "Loading...";
                break;
            default:
                loadingText.text = "Loading...";
                break;
        }

    }
}
