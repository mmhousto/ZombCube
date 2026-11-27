#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using Com.GCTC.ZombCube;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using System;
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
        if (string.IsNullOrEmpty(boardName)) return;
        // Several uploads to one board before it's found: only the latest score is sent.
        pendingScores[boardName] = score;
        WithBoard(boardName, (found, board) =>
        {
            if (!pendingScores.TryGetValue(boardName, out int latest)) return;
            pendingScores.Remove(boardName);
            if (found) Upload(board, latest);
        });
    }

    /// <summary>
    /// Downloads entries from a board for the in-game leaderboards page. <paramref name="done"/> gets the rows
    /// (empty if the board has no scores) or null if Steam couldn't be reached or the board doesn't exist.
    /// </summary>
    public void RequestEntries(string boardName, LeaderboardScope scope, int count, Action<List<LeaderboardRow>> done)
    {
        WithBoard(boardName, (found, board) =>
        {
            if (!found) { done?.Invoke(null); return; }

            ELeaderboardDataRequest type;
            int start, end;
            switch (scope)
            {
                case LeaderboardScope.Friends:
                    type = ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends; start = 1; end = count; break;
                case LeaderboardScope.AroundMe:
                    type = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser; start = -(count / 2); end = count - count / 2 - 1; break;
                default:
                    type = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal; start = 1; end = count; break;
            }

            var download = new CallResult<LeaderboardScoresDownloaded_t>();
            liveCalls.Add(download);
            download.Set(SteamUserStats.DownloadLeaderboardEntries(board, type, start, end), (result, failure) =>
            {
                liveCalls.Remove(download);
                if (failure) { done?.Invoke(null); return; }

                CSteamID me = SteamUser.GetSteamID();
                var rows = new List<LeaderboardRow>();
                for (int i = 0; i < result.m_cEntryCount; i++)
                {
                    SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, i, out LeaderboardEntry_t entry, null, 0);
                    rows.Add(new LeaderboardRow
                    {
                        rank = entry.m_nGlobalRank,
                        name = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser),
                        score = entry.m_nScore,
                        isMe = entry.m_steamIDUser == me
                    });
                }
                done?.Invoke(rows);
            });
        });
    }

    // Finds a board by name once, then runs every action waiting for it.
    private readonly Dictionary<string, List<Action<bool, SteamLeaderboard_t>>> waitingForBoard = new Dictionary<string, List<Action<bool, SteamLeaderboard_t>>>();

    private void WithBoard(string boardName, Action<bool, SteamLeaderboard_t> then)
    {
        if (string.IsNullOrEmpty(boardName) || !SteamManager.IsReady)
        {
            then(false, default);
            return;
        }

        if (foundBoards.TryGetValue(boardName, out SteamLeaderboard_t board))
        {
            then(true, board);
            return;
        }

        if (!waitingForBoard.TryGetValue(boardName, out var waiting))
            waitingForBoard[boardName] = waiting = new List<Action<bool, SteamLeaderboard_t>>();
        waiting.Add(then);

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
        bool found = !failure && pCallback.m_bLeaderboardFound != 0;
        if (found) foundBoards[boardName] = pCallback.m_hSteamLeaderboard;

        if (!waitingForBoard.TryGetValue(boardName, out var waiting)) return;
        waitingForBoard.Remove(boardName);
        foreach (var action in waiting) action(found, pCallback.m_hSteamLeaderboard);
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