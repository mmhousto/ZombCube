#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using Com.GCTC.ZombCube;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamLeaderboardManager : MonoBehaviour
{
#if !DISABLESTEAMWORKS
    private static SteamLeaderboardManager instance;

    public static SteamLeaderboardManager Instance { get { return instance; } }

    private SteamLeaderboard_t s_currentLeaderboard;
    private CallResult<LeaderboardScoresDownloaded_t> m_downloadResult = new CallResult<LeaderboardScoresDownloaded_t>();

    public enum LeaderboardName
    {
        BestAccuracy,
        CubesDestroyed,
        HighestPartyWave,
        HighestSoloWave,
        MostPoints
    }

    public struct LeaderboardData
    {
        public string username;
        public int rank;
        public int score;
    }
    List<LeaderboardData> LeaderboardDataset;

    // Steamworks names of the boards in LeaderboardName order. The per-map wave boards
    // (WaveLeaderboards) are looked up by name the same way.
    private static readonly string[] leaderboardNames =
    {
        "Best Accuracy",
        "Cubes Destroyed",
        "Highest Party Wave",
        "Highest Solo Wave",
        "Most Points"
    };

    // Boards found so far, by name; scores waiting for their board to be found.
    private readonly Dictionary<string, SteamLeaderboard_t> foundBoards = new Dictionary<string, SteamLeaderboard_t>();
    private readonly Dictionary<string, int> pendingScores = new Dictionary<string, int>();
    private readonly HashSet<string> findsInFlight = new HashSet<string>();
    // A CallResult only tracks one call, so each find/upload gets its own (kept alive until it completes).
    private readonly List<object> liveCalls = new List<object>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        Invoke(nameof(UpdateLeaderboards), 6);
    }

    private void UpdateLeaderboards()
    {
        LeaderboardManager.UpdateSteamLeaderboards();
    }

    public void UpdateScore(int score, LeaderboardName leaderboardName)
    {
        UpdateScore(score, leaderboardNames[(int)leaderboardName]);
    }

    /// <summary>Uploads a score to the Steam leaderboard with this name, finding the board first if needed.</summary>
    public void UpdateScore(int score, string boardName)
    {
        if (string.IsNullOrEmpty(boardName) || !SteamManager.IsReady) return;

        if (foundBoards.TryGetValue(boardName, out SteamLeaderboard_t board))
        {
            Upload(board, score);
            return;
        }

        // Keep the latest score; it's uploaded once the board is found.
        pendingScores[boardName] = score;
        if (findsInFlight.Add(boardName))
        {
            var find = new CallResult<LeaderboardFindResult_t>();
            liveCalls.Add(find);
            find.Set(SteamUserStats.FindLeaderboard(boardName), (result, failure) =>
            {
                liveCalls.Remove(find);
                findsInFlight.Remove(boardName);
                OnLeaderboardFindResult(boardName, result, failure);
            });
        }
    }

    private void OnLeaderboardFindResult(string boardName, LeaderboardFindResult_t pCallback, bool failure)
    {
        Debug.Log($"Steam Leaderboard Find '{boardName}': Did it fail? {failure}, Found: {pCallback.m_bLeaderboardFound}, leaderboardID: {pCallback.m_hSteamLeaderboard.m_SteamLeaderboard}");
        if (failure || pCallback.m_bLeaderboardFound == 0)
        {
            pendingScores.Remove(boardName);
            return;
        }

        foundBoards[boardName] = pCallback.m_hSteamLeaderboard;
        if (pendingScores.TryGetValue(boardName, out int score))
        {
            pendingScores.Remove(boardName);
            Upload(pCallback.m_hSteamLeaderboard, score);
        }
    }

    private void Upload(SteamLeaderboard_t board, int score)
    {
        var upload = new CallResult<LeaderboardScoreUploaded_t>();
        liveCalls.Add(upload);
        SteamAPICall_t call = SteamUserStats.UploadLeaderboardScore(board, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate, score, null, 0);
        upload.Set(call, (result, failure) =>
        {
            liveCalls.Remove(upload);
            OnLeaderboardUploadResult(result, failure);
        });
    }

    private void OnLeaderboardUploadResult(LeaderboardScoreUploaded_t pCallback, bool failure)
    {
        Debug.Log($"Steam Leaderboard Upload: Did it fail? {failure}, Score: {pCallback.m_nScore}, HasChanged: {pCallback.m_bScoreChanged}");
    }

    //change ELeaderboardDataRequest to get a different set (focused around player or global)
    public void GetLeaderBoardData(ELeaderboardDataRequest _type = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, int entries = 14)
    {
        SteamAPICall_t hSteamAPICall;
        switch (_type)
        {
            case ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal:
                hSteamAPICall = SteamUserStats.DownloadLeaderboardEntries(s_currentLeaderboard, _type, 1, entries);
                m_downloadResult.Set(hSteamAPICall, OnLeaderboardDownloadResult);
                break;
            case ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser:
                hSteamAPICall = SteamUserStats.DownloadLeaderboardEntries(s_currentLeaderboard, _type, -(entries / 2), (entries / 2));
                m_downloadResult.Set(hSteamAPICall, OnLeaderboardDownloadResult);
                break;
            case ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends:
                hSteamAPICall = SteamUserStats.DownloadLeaderboardEntries(s_currentLeaderboard, _type, 1, entries);
                m_downloadResult.Set(hSteamAPICall, OnLeaderboardDownloadResult);
                break;
        }
        //Note that the LeaderboardDataset will not be updated immediatly (see callback below)
    }

    private void OnLeaderboardDownloadResult(LeaderboardScoresDownloaded_t pCallback, bool failure)
    {
        Debug.Log($"Steam Leaderboard Download: Did it fail? {failure}, Result - {pCallback.m_hSteamLeaderboardEntries}");
        LeaderboardDataset = new List<LeaderboardData>();
        //Iterates through each entry gathered in leaderboard
        for (int i = 0; i < pCallback.m_cEntryCount; i++)
        {
            LeaderboardEntry_t leaderboardEntry;
            SteamUserStats.GetDownloadedLeaderboardEntry(pCallback.m_hSteamLeaderboardEntries, i, out leaderboardEntry, null, 0);
            //Example of how leaderboardEntry might be held/used
            LeaderboardData lD;
            lD.username = SteamFriends.GetFriendPersonaName(leaderboardEntry.m_steamIDUser);
            lD.rank = leaderboardEntry.m_nGlobalRank;
            lD.score = leaderboardEntry.m_nScore;
            LeaderboardDataset.Add(lD);
            Debug.Log($"User: {lD.username} - Score: {lD.score} - Rank: {lD.rank}");
        }
        //This is the callback for my own project - function is asynchronous so it must return from here rather than from GetLeaderBoardData
        //FindObjectOfType<HighscoreUIMan>().FillLeaderboard(LeaderboardDataset);
    }
#endif
}