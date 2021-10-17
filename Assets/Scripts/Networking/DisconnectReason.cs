namespace DapperDino.UMT.Lobby.Networking
{
    public class DisconnectReason
    {
        public ConnectStatus Reason { get; private set; } = ConnectStatus.Undefined;

        /// <summary>
        /// Sets the disconnect reason.
        /// </summary>
        /// <param name="reason"></param>
        public void SetDisconnectReason(ConnectStatus reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// Clears the disconnect reason to Undefined.
        /// </summary>
        public void Clear()
        {
            Reason = ConnectStatus.Undefined;
        }

        public bool HasTransitionReason => Reason != ConnectStatus.Undefined;
    }
}
