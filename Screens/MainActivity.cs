using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeeKee.Android.Adapters;
using VeeKee.Android.Model;
using VeeKee.Android.ViewModel;
using VeeKee.Shared.CountryCodeLookup;
using VeeKee.Shared.Models;
using VeeKee.Shared.Ssh;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using WidgetToolBar = Android.Support.V7.Widget.Toolbar;

namespace VeeKee.Android
{
    [Activity(Label = "VeeKee", MainLauncher = true, Icon = "@drawable/VeeKeeIcon")]
    public class MainActivity : AppCompatActivity
    {
        private WidgetToolBar _toolbar;

        private VeeKeePreferences _preferences;

        private WifiStateService _wifiState;
        private string _updatingMessage;
        private string _updatedMessage;
        private FrameLayout _progressBarFrameLayout;

        #region Activity Events
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set up default Shared Preferences (setting defaults if required)
            _preferences = new VeeKeePreferences(Application.Context);

            this.SetContentView(Resource.Layout.Main);

            _toolbar = FindViewById<WidgetToolBar>(Resource.Id.toolbar);
            SetSupportActionBar(_toolbar);

            _progressBarFrameLayout = FindViewById<FrameLayout>(Resource.Id.progressBarFrameLayout);

            var vpnListView = FindViewById<ListView>(Resource.Id.vpnListView);
            vpnListView.ItemClick += VpnListView_ItemClick;
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

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.settingsmenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }
        #endregion Activity Events

        #region Click Events
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.settings_button:
                    var preferencesIntent = new Intent(this, typeof(AppPreferencesActivity));
                    StartActivity(preferencesIntent);
                    return true;
                case Resource.Id.autoconfigure_button:
                    AutoConfigure();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private async void AutoConfigure()
        {
            // TODO check for internet connection
            if (!ConfirmWifi())
            {
                return;
            }

            var vpnListView = FindViewById<ListView>(Resource.Id.vpnListView);
            var vpnArrayAdapter = (VpnArrayAdapter)vpnListView.Adapter;

            vpnArrayAdapter.Enabled = false;
            _progressBarFrameLayout.Visibility = ViewStates.Visible;

            try
            {
                using (var asusCommander = GetAsusSshVpnCommander())
                {
                    // Get all the VPN Names and HostNames from the router
                    var success = await vpnArrayAdapter.VpnUIItemViewModel.AutoConfigureFromRouterSettings(asusCommander, _preferences);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, String.Format("Unhandled exception {0}", ex.ToString()), ToastLength.Long);
            }
            finally
            {
                // Ensure that the UI is updated
                vpnArrayAdapter.NotifyDataSetChanged();

                _progressBarFrameLayout.Visibility = ViewStates.Gone;
                vpnArrayAdapter.Enabled = true;
            }
        }

        private async void VpnListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Determine the wifi connection status of the device
            if (!ConfirmWifi())
            {
                return;
            }

            var vpnListView = (ListView)sender;
            var vpnRowItem = vpnListView.GetChildAt(e.Position);
            var vpnSwitch = (Switch)vpnRowItem.FindViewById(Resource.Id.vpnSwitch);

            bool tappedVpnCurrentlyEnabled = vpnSwitch.Checked;
            var vpnArrayAdapter = (VpnArrayAdapter)vpnListView.Adapter;

            // Ensure that the correct Vpn Switches are enabled and disabled accordingly
            UpdateSelectedVpnItems(vpnListView, e.Position);

            vpnArrayAdapter.Enabled = false;
            _progressBarFrameLayout.Visibility = ViewStates.Visible;

            bool success = false;
            var routerStatus = RouterConnectionStatus.NotConnected;
            try
            {
                using (var asusCommander = GetAsusSshVpnCommander())
                {
                    var vpnIndex = e.Position + 1;
                    var connected = await asusCommander.Connect();
                    routerStatus = asusCommander.Connection.Status;

                    switch (routerStatus)
                    {
                        case RouterConnectionStatus.Connected:
                            if (tappedVpnCurrentlyEnabled)
                            {
#if DEBUG
                                await Task.Delay(TimeSpan.FromSeconds(2));
#else
                                success = await asusCommander.DisableVpn(vpnIndex);
#endif
                            }
                            else
                            {
#if DEBUG
                                await Task.Delay(TimeSpan.FromSeconds(2));
#else
                                success = await asusCommander.EnableVpn(vpnIndex);
#endif
                            }
                            break;

                        default:
                            DisplayRouterIssueDialog(routerStatus);
                            return;
                    }

                    // TODO
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, String.Format("Unhandled exception {0}", ex.ToString()), ToastLength.Long);
            }
            finally
            {
                _progressBarFrameLayout.Visibility = ViewStates.Gone;
                vpnArrayAdapter.Enabled = true;
            }

            if (success)
            {
                var mainLayout = this.FindViewById<RelativeLayout>(Resource.Id.mainLayout);

                Snackbar.Make(mainLayout, _updatedMessage, Snackbar.LengthLong)
                    .SetAction(this.GetString(Resource.String.OkMessage), (view) => { })
                    .Show();
            }
            
        }
#endregion Click Events

        private bool ConfirmWifi()
        {
            _wifiState = new WifiStateService();
            if (!_wifiState.Connected)
            {
                DisplayWifiIssueDialog();
                return false;
            }

            return true;
        }

        async private Task InitialiseMainScreen()
        {
            bool initialisationIssue = true;

            // Determine the wifi connection status of the device
            if (!ConfirmWifi())
            {
                return;
            }

            // Initialise a list of VpnItems
            var vpnUIItemViewModel = new VpnUIItemViewModel(PackageName, this.Resources);
            vpnUIItemViewModel.LoadVpnUIItemsFromPreferences(_preferences);
            var vpnItems = vpnUIItemViewModel.VpnUIItems;

            var routerStatus = RouterConnectionStatus.NotConnected;

            using (var asusCommander = GetAsusSshVpnCommander())
            {
                bool connected = await asusCommander.Connect();

                routerStatus = asusCommander.Connection.Status;

                if (!asusCommander.Connection.IsConnected)
                {
                    DisplayRouterIssueDialog(routerStatus);
                    return;
                }

                _progressBarFrameLayout.Visibility = ViewStates.Visible;
                bool success = await vpnUIItemViewModel.UpdateVpnUIItemStatus(asusCommander);
                initialisationIssue = !success;
            }

            _progressBarFrameLayout.Visibility = ViewStates.Gone;

            var adapter = new VpnArrayAdapter(this, vpnUIItemViewModel);
            var vpnListView = FindViewById<ListView>(Resource.Id.vpnListView);
            vpnListView.Adapter = adapter;
            ((VpnArrayAdapter)vpnListView.Adapter).Enabled = !initialisationIssue;
            vpnListView.Enabled = !initialisationIssue;
        }

        private AsusSshVpnService GetAsusSshVpnCommander()
        {
            var asusCommander = new AsusSshVpnService(
                _preferences.RouterIpAddress,
                _preferences.RouterUsername,
                _preferences.RouterPassword,
                int.Parse(_preferences.RouterPort),
                this.Resources.GetInteger(Resource.Integer.ConnectionTimeoutSeconds));

            return asusCommander;
        }

        private void UpdateSelectedVpnItems(
            ListView vpnListView,
            int selectedIndex)
        {
            var format = string.Empty;

            var adapter = (VpnArrayAdapter)vpnListView.Adapter;

            for (int i = 0; i < vpnListView.Count; i++)
            {
                var vpnItem = adapter[i];

                if (i == selectedIndex)
                {
                    vpnItem.Status = vpnItem.Status == VpnStatus.Enabled ? VpnStatus.Off : VpnStatus.Enabled;

                    format = vpnItem.Status == VpnStatus.Enabled ? this.Resources.GetString(Resource.String.EnablingVpnFormat) : this.Resources.GetString(Resource.String.DisablingVpnFormat);
                    _updatingMessage = String.Format(format, (string)vpnItem.Name);

                    format = vpnItem.Status == VpnStatus.Enabled ? this.Resources.GetString(Resource.String.EnabledVpnFormat) : this.Resources.GetString(Resource.String.DisabledVpnFormat);
                    _updatedMessage = String.Format(format, (string)vpnItem.Name);
                }
                else
                {
                    vpnItem.Status = VpnStatus.Off;
                }
            }

            // Ensure that the UI is updated
            adapter.NotifyDataSetChanged();
        }

        private void DisplayWifiIssueDialog()
        {
            string dialogTitle = this.Resources.GetString(Resource.String.OopsMessage);
            string dialogMessage = String.Empty;

            dialogMessage = this.Resources.GetString(Resource.String.NotConnectedToWifiMessage);

            // Display dialog to notify the user that they are not connected to a Wifi network
            var builder = new AlertDialog.Builder(this)
            .SetTitle(dialogTitle)
            .SetMessage(dialogMessage)
            .SetPositiveButton("Settings", delegate
            {
                var preferencesIntent = new Intent(Application.Context, typeof(AppPreferencesActivity));
                StartActivity(preferencesIntent);
            })
            .SetNegativeButton("Quit", delegate
            {
                this.Finish();
            })
            .SetCancelable(true)
            .Show();
        }

        private void DisplayRouterIssueDialog(RouterConnectionStatus routerStatus)
        {
            string dialogTitle = this.Resources.GetString(Resource.String.OopsMessage);
            string dialogMessage = String.Empty;

            // Router connection issue?
            if (routerStatus != RouterConnectionStatus.Connected)
            {
                switch (routerStatus)
                {
                    case RouterConnectionStatus.AuthorizationError:
                        dialogMessage = this.Resources.GetString(Resource.String.RouterConnectionAuthorizationIssueMessage);
                        break;
                    case RouterConnectionStatus.NetworkError:
                    case RouterConnectionStatus.ConnectionTimeoutError:
                        dialogMessage = this.Resources.GetString(Resource.String.NetworkIssueMessage);
                        break;
                    default:
                        dialogMessage = this.Resources.GetString(Resource.String.RouterNotConnectedMessage);
                        break;
                }

                var builder = new AlertDialog.Builder(this)
                .SetTitle(dialogTitle)
                .SetMessage(dialogMessage)
                .SetPositiveButton("Settings", delegate
                {
                    var preferencesIntent = new Intent(Application.Context, typeof(AppPreferencesActivity));
                    StartActivity(preferencesIntent);
                })
                .SetNegativeButton("Quit", delegate
                {
                    this.Finish();
                })
                .SetCancelable(true)
                .Show();
            }
        }
    }
}

