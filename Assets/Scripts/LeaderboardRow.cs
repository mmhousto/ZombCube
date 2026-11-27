namespace Com.GCTC.ZombCube
{
    /// <summary>Which slice of a leaderboard to show.</summary>
    public enum LeaderboardScope
    {
        Top,
        Friends,
        AroundMe
    }

    /// <summary>One line on the in-game leaderboards page.</summary>
    public struct LeaderboardRow
    {
        public int rank;
        public string name;
        public int score;
        public bool isMe;
    }
}
