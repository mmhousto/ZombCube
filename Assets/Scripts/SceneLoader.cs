using UnityEngine.SceneManagement;

public static class SceneLoader
{
    
    public static void ToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public static void ToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public static void ToLoading()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    public static void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

}
