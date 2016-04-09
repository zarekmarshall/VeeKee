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

    public class ConnectionResult
    {
        public ConnectionStatus Status { get; set; }
        public Exception Error { get; set; }
    }
}