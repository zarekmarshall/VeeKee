using Android.App;
using Android.Content;
using Android.Net;
using System.Linq;
using VeeKee.Shared.Wifi;

namespace VeeKee.Android
{
    public class WifiStateService : IWifiStateService
    {
        private Context _applicationContext;

        public WifiStateService()
        {
            this._applicationContext = Application.Context;
        }

        public bool Connected
        {
            get
            {
                var connectivityManager = (ConnectivityManager)this._applicationContext.GetSystemService("connectivity");

                var networks = connectivityManager.GetAllNetworks();
                var wifiNetworkInfo = (from w in (from n in networks
                                                  select connectivityManager.GetNetworkInfo(n))
                                       where w.Type == ConnectivityType.Wifi
                                       select w).FirstOrDefault();

                var wifiConnected = wifiNetworkInfo != null && wifiNetworkInfo.IsConnected;

                return wifiConnected;
            }
        }
    }


}