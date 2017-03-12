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
            var networkInfo = connectivityManager.ActiveNetworkInfo;

            var wifiConnected = networkInfo != null && networkInfo.Type == ConnectivityType.Wifi && networkInfo.IsConnected;

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