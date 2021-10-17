namespace DapperDino.UMT.Lobby.Networking
{
    /// <summary>
    /// List of connection status'
    /// </summary>
    public enum ConnectStatus
    {
        Undefined,
        Success,
        ServerFull,
        GameInProgress,
        LoggedInAgain,
        UserRequestedDisconnect,
        GenericDisconnect,
        WrongPassword
    }
}
