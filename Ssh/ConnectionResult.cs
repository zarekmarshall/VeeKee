using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace VeeKee.Ssh
{
    public enum ConnectionStatus
    {
        NotConnected = 0,
        Connected = 1,
        AuthorizationError = 2
    }

    public enum CommandStatus
    {
        Fail = 0,
        Success = 1
    }

    public abstract class SshResult
    {
        public Exception Error { get; set; }
    }


    public class ConnectionResult : SshResult
    {
        public ConnectionStatus Status { get; set; }
        
        public bool IsConnected
        {
            get
            {
                return this.Status == ConnectionStatus.Connected;
            }
        }
    }

    public class CommandResult : SshResult
    {
        public CommandStatus Status { get; set; }
        public string Result { get; set; }
    }
}