using Android.App;
using Android.Net;
using Android.OS;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VeeKee.Adapters;
using VeeKee.Ssh;
using Android.Views;
using Android.Content;

namespace VeeKee
{
    [Activity(Label = "VeeKee", MainLauncher = true, Icon = "@drawable/VeeKeeIcon")]
    public class MainActivity : Activity
    {
        private VeeKeePreferences _preferences;
        private RouterConnectionStatusViewer _routerStatusViewer;

        public enum VeeKeeWifiStatus
        {
            Off = 0,
            Connected = 1,
            ConnectedToCorrectWifi = 2
        }

        private VeeKeeWifiStatus _wifiStatus;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set up default Shared Preferences (setting defaults if required)
            _preferences = new VeeKeePreferences(Application.Context);

            _routerStatusViewer = new RouterConnectionStatusViewer(this);

            this.SetContentView(Resource.Layout.Main);

            // Set up the Action Bar
            ActionBar.DisplayOptions = ActionBarDisplayOptions.UseLogo | ActionBarDisplayOptions.ShowHome | ActionBarDisplayOptions.ShowTitle;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.settingsmenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.settings_button:
                    var preferencesIntent = new Intent(this, typeof(AppPreferencesActivity));
                    StartActivity(preferencesIntent);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        async protected override void OnResume()
        {
            base.OnResume();

            // Connection Toolbar should be off by default
            _routerStatusViewer.Hide();

            if (_preferences.FirstRun)
            {
                // Direct user to settings
                var preferencesIntent = new Intent(Application.Context, typeof(AppPreferencesActivity));
                StartActivity(preferencesIntent);

                _preferences.FirstRun = false;
                _preferences.Save();
                return;
            }

            await InitialiseMainScreen();
        }

        async private Task InitialiseMainScreen()
        {
            // Determine the wifi connection status of the device
            DetectWifiStatus();

            // Initialise a list of VpnItems
            var vpnItems = VpnItems();

            RouterConnectionStatus routerStatus = RouterConnectionStatus.NotConnected;
            if (_wifiStatus == VeeKeeWifiStatus.ConnectedToCorrectWifi)
            {
                // Check the current status of vpns
                var progressDialog = new ProgressDialog(this);
                progressDialog.SetTitle(this.GetString(Resource.String.CheckingVpnStatusMessage));
                progressDialog.SetMessage(this.GetString(Resource.String.ConnectingToRouterStatusMessage));
                progressDialog.Show();

                using (var asusCommander = GetAsusSshVpnCommander())
                {
                    bool connected = await asusCommander.Connect();

                    routerStatus = asusCommander.Connection.Status;

                    if (asusCommander.Connection.IsConnected)
                    {
                        progressDialog.SetMessage(this.GetString(Resource.String.CheckingVpnStatusMessage));
                        vpnItems = await CheckCurrentVpnStatus(asusCommander, vpnItems);
                    }
                }

                progressDialog.Dismiss();
            }

            _routerStatusViewer.Display(this._wifiStatus, routerStatus, ConnectionToolbar_MenuItemClick);

            var adapter = new VpnArrayAdapter(this, vpnItems);
            var vpnListView = FindViewById<ListView>(Resource.Id.vpnListView);
            vpnListView.Adapter = adapter;
            vpnListView.Enabled = _wifiStatus == VeeKeeWifiStatus.ConnectedToCorrectWifi && !_routerStatusViewer.IsVisible;

            vpnListView.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(VpnListView_ItemClick);
        }

        private async void ConnectionToolbar_MenuItemClick(object sender, Toolbar.MenuItemClickEventArgs e)
        {
            await InitialiseMainScreen();
        }

        private void DetectWifiStatus()
        {
            _wifiStatus = VeeKeeWifiStatus.Off;

            // Check if we are connected to wifi
            string currentWifiName = string.Empty;
            if (ConnectedToWifi(out currentWifiName))
            {
                _wifiStatus = VeeKeeWifiStatus.Connected;

                if (_preferences.WifiName == string.Empty)
                {
                    // We haven't connected to Wifi before so lets
                    // save the name of the Wifi we are connected to 
                    // (which we will use to check we are connected to the correct Wifi next time)
                    _preferences.WifiName = currentWifiName;
                    _preferences.Save();
                }

                // Check that we are connected to the right Wifi connection
                if (ConnectedToCorrectWifi(_preferences.WifiName))
                {
                    _wifiStatus = VeeKeeWifiStatus.ConnectedToCorrectWifi;
                }
            }
        }

        private Dictionary<int, VpnItem> VpnItems()
        {
            var vpnItems = new Dictionary<int, VpnItem>();

            for (int i = 1; i<this.Resources.GetInteger(Resource.Integer.VpnItemCount) + 1; i++)
            {
                var vpnItemName = string.Format(this.Resources.GetString(Resource.String.VpnClientFormat), i);

                vpnItems.Add(i, new VpnItem(vpnItemName, GetFlagResourceId(_preferences.GetVpnFlag(i)), VpnStatus.Off));
            }

            return vpnItems;
        }

        private int GetFlagResourceId(string flagName)
        {
            int resourceId = Resources.GetIdentifier(flagName.ToLower(), "drawable", PackageName);

            if (resourceId == 0)
            {
                resourceId = Resource.Drawable.DEFAULTFLAG;
            }

            return resourceId;
        }

        private AsusSshVpnCommander GetAsusSshVpnCommander()
        {
            var asusCommander = new AsusSshVpnCommander(
                _preferences.RouterIpAddress,
                _preferences.RouterUsername,
                _preferences.RouterPassword,
                int.Parse(_preferences.RouterPort),
                this.Resources.GetInteger(Resource.Integer.DefaultRouterSshPort));

            return asusCommander;
        }

        private async Task<Dictionary<int, VpnItem>> CheckCurrentVpnStatus(AsusSshVpnCommander asusCommander, Dictionary<int, VpnItem> vpnItems)
        {
            if (asusCommander.Connection.IsConnected)
            {
                var vpnStatus = await asusCommander.Status();

                foreach (var key in vpnStatus.Keys)
                {
                    vpnItems[key].Status = vpnStatus[key];
                }
            }

            return vpnItems;            
        }

        private string UpdateSelectedVpnItems(
            ListView vpnListView,
            int selectedIndex)
        {
            var updatingFormat = string.Empty;
            var updatingMessage = string.Empty;

            // Ensure that the correct Vpn Switches are enabled and disabled accordingly
            for (int i = 0; i < vpnListView.Count; i++)
            {
                var vpnRowItem = vpnListView.GetChildAt(i);
                var vpnSwitch = (Switch)vpnRowItem.FindViewById(Resource.Id.vpnSwitch);
                var vpnName = (TextView)vpnRowItem.FindViewById(Resource.Id.vpnName);

                if (i == selectedIndex)
                {
                    vpnSwitch.Checked = !vpnSwitch.Checked;
                    updatingFormat = vpnSwitch.Checked ? this.Resources.GetString(Resource.String.EnablingVpnFormat) : this.Resources.GetString(Resource.String.DisablingVpnFormat);
                    updatingMessage = String.Format(updatingFormat, (string)vpnName.Text);
                }
                else
                {
                    vpnSwitch.Checked = false;
                }
            }

            return updatingMessage;
        }

        private async void VpnListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            using (var progressDialog = new ProgressDialog(this))
            {
                var vpnListView = (ListView)sender;

                // Ensure that the correct Vpn Switches are enabled and disabled accordingly
                var updatingMessage = UpdateSelectedVpnItems(vpnListView, e.Position);

                // TODO
                progressDialog.SetTitle(this.Resources.GetString(Resource.String.UpdatingVpnTitle));
                progressDialog.SetMessage(updatingMessage);
                progressDialog.Show();

                RouterConnectionStatus routerStatus = RouterConnectionStatus.NotConnected;
                try
                {
                    using (var asusCommander = GetAsusSshVpnCommander())
                    {
                        var vpnIndex = e.Position + 1;
                        var connected = await asusCommander.Connect();
                        routerStatus = asusCommander.Connection.Status;

                        if (routerStatus == RouterConnectionStatus.Connected)
                        {
                            // TODO
                            var success = await asusCommander.EnableVpn(vpnIndex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, String.Format("Unhandled exception {0}", ex.ToString()), ToastLength.Long);
                }
                finally
                {
                    progressDialog.Dismiss();
                }

                if (routerStatus != RouterConnectionStatus.Connected)
                {
                    _routerStatusViewer.Display(this._wifiStatus, routerStatus, ConnectionToolbar_MenuItemClick);
                }
            }
        }

        private bool ConnectedToWifi(out string wifiName)
        {
            wifiName = string.Empty;

            var connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
            var networkInfo = connectivityManager.ActiveNetworkInfo;

            var wifiConnected = networkInfo != null && networkInfo.IsConnected;

            if (wifiConnected)
            {
                wifiName = networkInfo.ExtraInfo.Trim().Replace("\"", "");
            }

            return wifiConnected;
        }

        private bool ConnectedToCorrectWifi(string correctWifiName)
        {
            string currentWifiName = string.Empty;
            bool wifiConnected = ConnectedToWifi(out currentWifiName);

            return wifiConnected && currentWifiName == correctWifiName;
        }
    }
}

