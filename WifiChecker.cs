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
using Android.Net;

namespace VeeKee
{
    public enum WifiStatus
    {
        NotConnected = 0,
        Connected = 1,
        ConnectedToExpectedWifi = 2
    }

    public class WifiChecker
    {
        private Context _applicationContext;

        public string ConnectedToWifiName { get; private set; }

        public bool Connected
        {
            get
            {
                return ConnectedToWifiName.Length > 0;
            }
        }

        public WifiStatus Status { get; private set; }

        public WifiChecker(Context context)
        {
            this._applicationContext = context;
            this.Status = WifiStatus.NotConnected;
            this.ConnectedToWifiName = string.Empty;
        }

        public void Check(string expectedWifiName)
        {
            var connectivityManager = (ConnectivityManager)this._applicationContext.GetSystemService("connectivity");

            // I can't figure out how to check for Wifi connection without using the deprecated
            // GetNetworkInfo method. ActiveNetworkInfo works, but only if Wifi internet is working
            // otherwise mobile data becomes the active network, if enabled on the device.
            var networkInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Wifi);
            
            var wifiConnected = networkInfo != null && networkInfo.IsConnected;

            if (wifiConnected)
            {
                this.Status = WifiStatus.Connected;
                this.ConnectedToWifiName = networkInfo.ExtraInfo.Trim().Replace("\"", "");

                if (expectedWifiName == String.Empty || this.ConnectedToWifiName.CompareTo(expectedWifiName) == 0)
                {
                    this.Status = WifiStatus.ConnectedToExpectedWifi;
                }
            }
        }
    }


}