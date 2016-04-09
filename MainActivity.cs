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
        private bool _connectedToWifi = false;
        private bool _connectedToCorrectWifi = false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            // Set up default Shared Preferences (setting defaults if required)
            _preferences = new VeeKeePreferences(Application.Context);

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

            // Show or hide the Connection Toolbar as required
            var connectionToolbar = FindViewById<Toolbar>(Resource.Id.connectiontoolbar);
            SetupConnectionToolbar(connectionToolbar);

            // Initialise a list of VpnItems
            var vpnItems = VpnItems();

            if (_connectedToCorrectWifi)
            {
                // Check the current status of vpns
                var progressDialog = new ProgressDialog(this);
                progressDialog.SetTitle(this.GetString(Resource.String.CheckingVpnStatusTitle));
                progressDialog.SetMessage(this.GetString(Resource.String.CheckingVpnStatusMessage));
                progressDialog.Show();

                vpnItems = await CheckCurrentVpnStatus(vpnItems);

                progressDialog.Dismiss();
            }

            var adapter = new VpnArrayAdapter(this, vpnItems);
            var vpnListView = FindViewById<ListView>(Resource.Id.vpnListView);
            vpnListView.Adapter = adapter;
            vpnListView.Enabled = _connectedToCorrectWifi;

            vpnListView.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(VpnListView_ItemClick);
        }

        private void SetupConnectionToolbar(Toolbar connectionToolbar)
        {
            if (!(_connectedToWifi || _connectedToCorrectWifi))
            {
                // Display the Connection Toolbar if we are not connected to wifi or the right wifi
                connectionToolbar.Title = this.Resources.GetString(Resource.String.NotConnectedToWifiMessage);
                connectionToolbar.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                connectionToolbar.Visibility = Android.Views.ViewStates.Gone;
            }
            connectionToolbar.InflateMenu(Resource.Menu.connectionrefreshmenu);
            connectionToolbar.MenuItemClick += ConnectionToolbar_MenuItemClick;
        }

        private async void ConnectionToolbar_MenuItemClick(object sender, Toolbar.MenuItemClickEventArgs e)
        {
            await InitialiseMainScreen();
        }

        private void DetectWifiStatus()
        {
            // Check if we are connected to wifi
            string currentWifiName = string.Empty;
            _connectedToWifi = ConnectedToWifi(out currentWifiName);

            if (_connectedToWifi)
            {
                if (_preferences.WifiName == string.Empty)
                {
                    // We haven't connected to Wifi before so lets
                    // save the name of the Wifi we are connected to 
                    // (which we will use to check we are connected to the correct Wifi next time)
                    _preferences.WifiName = currentWifiName;
                    _preferences.Save();
                }

                // Check that we are connected to the right Wifi connection
                _connectedToCorrectWifi = ConnectedToCorrectWifi(_preferences.WifiName);
            }
        }

        private Dictionary<int, VpnItem> VpnItems()
        {
            // TODO
            var vpnItems = new Dictionary<int, VpnItem>();
            vpnItems.Add(1, new VpnItem("Vpn 1", 0, VpnStatus.Off));
            vpnItems.Add(2, new VpnItem("Vpn 2", 0, VpnStatus.Off));
            vpnItems.Add(3, new VpnItem("Vpn 3", 0, VpnStatus.Off));
            vpnItems.Add(4, new VpnItem("Vpn 4", 0, VpnStatus.Off));
            vpnItems.Add(5, new VpnItem("Vpn 5", 0, VpnStatus.Off));

            return vpnItems;
        }

        private async Task<Dictionary<int, VpnItem>> CheckCurrentVpnStatus(Dictionary<int, VpnItem> vpnItems)
        {
            using (var asusCommander = new AsusSshVpnCommander(
                _preferences.RouterIpAddress, 
                _preferences.RouterUsername,
                _preferences.RouterPassword,
                _preferences.RouterPort))
            {
                ConnectionResult connectionResult = null;
                try
                {
                    connectionResult = await asusCommander.Connect();
                }
                catch (Exception ex)
                {
                    // TODO
                }

                if (connectionResult != null && connectionResult.Status == ConnectionStatus.Connected)
                {

                    var vpnStatus = await asusCommander.Status();

                    foreach (var key in vpnStatus.Keys)
                    {
                        vpnItems[key].Status = vpnStatus[key];
                    }
                }

                return vpnItems;
            }
        }

        private async void VpnListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            using (var progressDialog = new ProgressDialog(this))
            {
                var vpnListView = (ListView)sender;
                var updatingMessage = string.Empty;

                // Ensure that the correct Vpn Switches are enabled and disabled accordingly
                for (int i = 0; i < vpnListView.Count; i++)
                {
                    var vpnRowItem = vpnListView.GetChildAt(i);
                    var vpnSwitch = (Switch)vpnRowItem.FindViewById(Resource.Id.vpnSwitch);
                    var vpnName = (TextView)vpnRowItem.FindViewById(Resource.Id.vpnName);

                    if (i == e.Position)
                    {
                        vpnSwitch.Checked = !vpnSwitch.Checked;
                        updatingMessage = vpnSwitch.Checked ? this.Resources.GetString(Resource.String.EnablingVpnMessage) : this.Resources.GetString(Resource.String.DisablingVpnMessage);
                        updatingMessage = String.Format("{0} {1}", updatingMessage, (string)vpnName.Text);
                    }
                    else
                    {
                        vpnSwitch.Checked = false;
                    }
                }

                // TODO
                progressDialog.SetTitle(this.Resources.GetString(Resource.String.UpdatingVpnTitle));
                progressDialog.SetMessage(updatingMessage);
                progressDialog.Show();

                try
                {
                    // TODO
                    using (var asusCommander = new AsusSshVpnCommander(
                        _preferences.RouterIpAddress,
                        _preferences.RouterUsername,
                        _preferences.RouterPassword,
                        _preferences.RouterPort))
                    {
                        var vpnIndex = e.Position + 1;
                        var connected = await asusCommander.Connect();

                        //var success = await asusCommander.EnableVpn(vpnIndex);
                    }
                }
                finally
                {
                    progressDialog.Dismiss();
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

