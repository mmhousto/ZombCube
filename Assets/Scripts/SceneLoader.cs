using UnityEngine.SceneManagement;

public static class SceneLoader
{
    
    public static void ToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public static void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

}
