using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Threading.Tasks;
using VeeKee.Android.Adapters;
using VeeKee.Android.ViewModel;
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

        private ListView _vpnListView;

        private SwipeRefreshLayout _vpnListViewSwipRefresh;

        private AsusSshVpnService AsusCommander
        {
            get
            {
                return new AsusSshVpnService(
                _preferences.RouterIpAddress,
                _preferences.RouterUsername,
                _preferences.RouterPassword,
                int.Parse(_preferences.RouterPort),
                this.Resources.GetInteger(Resource.Integer.ConnectionTimeoutSeconds));
            }
        }

        #region Activity Events
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Xamarin.Essentials.Platform.Init(this, bundle);

            // Set up default Shared Preferences (setting defaults if required)
            _preferences = new VeeKeePreferences();

            this.SetContentView(Resource.Layout.Main);

            _toolbar = FindViewById<WidgetToolBar>(Resource.Id.toolbar);
            SetSupportActionBar(_toolbar);

            _progressBarFrameLayout = FindViewById<FrameLayout>(Resource.Id.progressBarFrameLayout);

            _vpnListView = FindViewById<ListView>(Resource.Id.vpnListView);
            _vpnListView.ItemClick += VpnListView_ItemClick;

            _vpnListViewSwipRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.vpnListViewSwipRefresh);
            _vpnListViewSwipRefresh.Refresh += VpnListViewSwipRefresh_Refresh;
        }

        private void VpnListViewSwipRefresh_Refresh(object sender, EventArgs e)
        {
            ToggleAppControls(false);

            Task.Run(async () =>
            {
                var vpnUIItemViewModel = (_vpnListView.Adapter as VpnArrayAdapter).VpnUIItemViewModel;

                using (var asusCommander = AsusCommander)
                {
                    bool success = await asusCommander.Connect();
                    success = await vpnUIItemViewModel.UpdateVpnUIItemStatus(asusCommander);
                    (_vpnListView.Adapter as VpnArrayAdapter).NotifyDataSetChanged();
                }
            }).Wait();

            ToggleAppControls(true);
            ((SwipeRefreshLayout)sender).Refreshing = false;
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
            if (!ConfirmWifi())
            {
                return;
            }

            ToggleAppControls(false);

            try
            {
                using (var asusCommander = AsusCommander)
                {
                    // Get all the VPN Names and HostNames from the router
                    var success = await (_vpnListView.Adapter as VpnArrayAdapter).VpnUIItemViewModel.AutoConfigureFromRouterSettings(asusCommander, _preferences);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, String.Format("Unhandled exception {0}", ex.ToString()), ToastLength.Long);
            }
            finally
            {
                // Ensure that the UI is updated
                (_vpnListView.Adapter as VpnArrayAdapter).NotifyDataSetChanged();

                ToggleAppControls(true);
            }
        }

        private async void VpnListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Determine the wifi connection status of the device
            if (!ConfirmWifi())
            {
                return;
            }

            var vpnRowItem = _vpnListView.GetChildAt(e.Position);
            var vpnSwitch = (Switch)vpnRowItem.FindViewById(Resource.Id.vpnSwitch);

            bool tappedVpnCurrentlyEnabled = vpnSwitch.Checked;

            ToggleAppControls(false);

            bool success = false;
            var routerStatus = RouterConnectionStatus.NotConnected;
            try
            {
                using (var asusCommander = AsusCommander)
                {
                    string selectedVpnItemName = (_vpnListView.Adapter as VpnArrayAdapter)[e.Position].Name;
                    var vpnIndex = e.Position + 1;
                    var connected = await asusCommander.Connect();
                    routerStatus = asusCommander.Connection.Status;
                    
                    switch (routerStatus)
                    {
                        case RouterConnectionStatus.Connected:
                            if (tappedVpnCurrentlyEnabled)
                            {
                                _updatingMessage = String.Format(this.Resources.GetString(Resource.String.DisablingVpnFormat), selectedVpnItemName);
                                _updatedMessage = String.Format(this.Resources.GetString(Resource.String.DisabledVpnFormat), selectedVpnItemName);
                                success = await asusCommander.DisableVpn(vpnIndex);
                                //await Task.Delay(TimeSpan.FromSeconds(3));
                            }
                            else
                            {
                                _updatingMessage = String.Format(this.Resources.GetString(Resource.String.EnablingVpnFormat), selectedVpnItemName);
                                _updatedMessage = String.Format(this.Resources.GetString(Resource.String.EnabledVpnFormat), selectedVpnItemName);
                                success = await asusCommander.EnableVpn(vpnIndex);
                                //await Task.Delay(TimeSpan.FromSeconds(3));
                            }

                            success = await (_vpnListView.Adapter as VpnArrayAdapter).VpnUIItemViewModel.UpdateVpnUIItemStatus(asusCommander);
                            (_vpnListView.Adapter as VpnArrayAdapter).NotifyDataSetChanged();
                            break;

                        default:
                            DisplayRouterIssueDialog(routerStatus);
                            return;
                    }

                    // TODO - handle non success
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, String.Format("Unhandled exception {0}", ex.ToString()), ToastLength.Long);
            }
            finally
            {
                ToggleAppControls(true);
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

        private void ToggleAppControls(bool enabled)
        {
            (_vpnListView.Adapter as VpnArrayAdapter).Enabled = enabled;
            _progressBarFrameLayout.Visibility = enabled ? ViewStates.Gone : ViewStates.Visible;
        }

        private async Task InitialiseMainScreen()
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

            using (var asusCommander = AsusCommander)
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

