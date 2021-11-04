
namespace DapperDino.UMT.Lobby.UI
{
    public struct LobbyPlayerState
    {
        /// <summary>
        /// Information needed for lobby player card.
        /// </summary>
        public int ActorId;
        public string PlayerName;
        public bool IsReady;
        public int CurrentBlaster;

        /// <summary>
        /// Creates lobby player state to hold information of player and display on card.
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="playerName"></param>
        /// <param name="isReady"></param>
        /// <param name="currentBlaster"></param>
        public LobbyPlayerState(int actorId, string playerName, bool isReady, int currentBlaster)
        {
            ActorId = actorId;
            PlayerName = playerName;
            IsReady = isReady;
            CurrentBlaster = currentBlaster;
        }

        /// <summary>
        /// Serializes the information over the network.
        /// </summary>
        /// <param name="serializer"></param>
        /*public void NetworkSerialize(NetworkSerializer serializer)
        {
            serializer.Serialize(ref ClientId);
            serializer.Serialize(ref PlayerName);
            serializer.Serialize(ref IsReady);
            serializer.Serialize(ref CurrentBlaster);
        }*/
    }
}