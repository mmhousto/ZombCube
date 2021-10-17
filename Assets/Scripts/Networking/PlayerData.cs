namespace DapperDino.UMT.Lobby.Networking
{
    public struct PlayerData
    {
        public string PlayerName { get; private set; } // Property for player name.
        public ulong ClientId { get; private set; } // Property for client id.
        public int CurrentBlaster { get; private set; } // Property for current blaster selected.

        /// <summary>
        /// Constructor that assignes values on creation.
        /// </summary>
        /// <param name="playerName">Players name.</param>
        /// <param name="clientId">Client Id.</param>
        /// <param name="currentBlaster">Current Blaster Index</param>
        public PlayerData(string playerName, ulong clientId, int currentBlaster)
        {
            PlayerName = playerName;
            ClientId = clientId;
            CurrentBlaster = currentBlaster;
        }
    }
}
