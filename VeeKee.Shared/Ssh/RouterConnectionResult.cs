using System;

namespace VeeKee.Shared.Ssh
{
    public enum RouterConnectionStatus
    {
        NotConnected = 0,
        Connected = 1,
        AuthorizationError = 2,
        NetworkError = 3,
        ConnectionTimeoutError = 4
    }

    public enum RouterCommandStatus
    {
        Fail = 0,
        Success = 1
    }

    public abstract class SshResult
    {
        public Exception Error { get; set; }
    }


    public class RouterConnectionResult : SshResult
    {
        public RouterConnectionStatus Status { get; set; }
        
        public bool IsConnected
        {
            get
            {
                return this.Status == RouterConnectionStatus.Connected;
            }
        }
    }

    public class RouterCommandResult : SshResult
    {
        public RouterCommandStatus Status { get; set; }
        public string Result { get; set; }
    }
}