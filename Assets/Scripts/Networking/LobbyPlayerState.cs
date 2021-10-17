using MLAPI.Serialization;

namespace DapperDino.UMT.Lobby.UI
{
    public struct LobbyPlayerState : INetworkSerializable
    {
        public ulong ClientId;
        public string PlayerName;
        public bool IsReady;
        public int CurrentBlaster;

        public LobbyPlayerState(ulong clientId, string playerName, bool isReady, int currentBlaster)
        {
            ClientId = clientId;
            PlayerName = playerName;
            IsReady = isReady;
            CurrentBlaster = currentBlaster;
        }

        public void NetworkSerialize(NetworkSerializer serializer)
        {
            serializer.Serialize(ref ClientId);
            serializer.Serialize(ref PlayerName);
            serializer.Serialize(ref IsReady);
            serializer.Serialize(ref CurrentBlaster);
        }
    }
}