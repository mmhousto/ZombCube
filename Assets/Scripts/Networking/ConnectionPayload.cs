using System;

namespace DapperDino.UMT.Lobby.Networking
{
    [Serializable]
    public class ConnectionPayload
    {
        /// <summary>
        /// Values to pass over the network.
        /// </summary>
        public string clientGUID;
        public int clientScene = -1;
        public string playerName;
        public int currentBlaster;
        public string password = "";
    }
}
