namespace Com.GCTC.ZombCube
{
    /// <summary>Photon room property keys for the map, texture pack and mode the host picked.</summary>
    public static class RoomLevelKeys
    {
        public const string Map = "Map";
        public const string TexturePack = "TexturePack";
        /// <summary>Hardcore Party: the game ends as soon as one player is eliminated.</summary>
        public const string Hardcore = "Hardcore";

        /// <summary>True when the current Photon room is set to Hardcore.</summary>
        public static bool IsHardcoreRoom
        {
            get
            {
                var room = Photon.Pun.PhotonNetwork.CurrentRoom;
                return room != null && room.CustomProperties.TryGetValue(Hardcore, out object value) && value is bool on && on;
            }
        }
    }
}
