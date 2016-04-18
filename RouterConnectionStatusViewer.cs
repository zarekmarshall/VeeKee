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
using VeeKee.Ssh;
using static VeeKee.MainActivity;

namespace VeeKee
{
    public class RouterConnectionStatusViewer
    {
        private Activity _contextActivity;
        private bool _isVisible = false;

        public RouterConnectionStatusViewer(Activity contextActivity)
        {
            this._contextActivity = contextActivity;
        }


        public void Hide()
        {
            var connectionToolbar = ConnectionStatusToolbar;
            connectionToolbar.Visibility = Android.Views.ViewStates.Gone;
        }

        public void Display(
            VeeKeeWifiStatus wifiStatus,
            RouterConnectionStatus status,
            EventHandler<Toolbar.MenuItemClickEventArgs> menuItemClick
            )
        {
            var connectionToolbar = ConnectionStatusToolbar;

            connectionToolbar.Visibility = Android.Views.ViewStates.Visible;

            if (wifiStatus == VeeKeeWifiStatus.Off)
            {
                connectionToolbar.Title = this._contextActivity.Resources.GetString(Resource.String.NotConnectedToWifiMessage);
            }
            else if (wifiStatus != VeeKeeWifiStatus.ConnectedToCorrectWifi)
            {
                // TODO
            }
            else
            {
                switch (status)
                {
                    case RouterConnectionStatus.AuthorizationError:
                        connectionToolbar.Title = this._contextActivity.Resources.GetString(Resource.String.RouterConnectionAuthorizationIssueMessage);
                        break;
                    case RouterConnectionStatus.NetworkError:
                        connectionToolbar.Title = this._contextActivity.Resources.GetString(Resource.String.NetworkIssueMessage);
                        break;
                    case RouterConnectionStatus.ConnectionTimeoutError:
                        connectionToolbar.Title = this._contextActivity.Resources.GetString(Resource.String.NetworkIssueMessage);
                        break;
                    case RouterConnectionStatus.Connected:
                        this.Hide();
                        break;
                }
            }

            // Add the refresh button if it isn't already visible
            if (!connectionToolbar.Menu.HasVisibleItems)
            {
                connectionToolbar.InflateMenu(Resource.Menu.connectionrefreshmenu);
                connectionToolbar.MenuItemClick += menuItemClick;
            }
        }

        private Toolbar ConnectionStatusToolbar
        {
            get
            {
                var connectionToolbar = this._contextActivity.FindViewById<Toolbar>(Resource.Id.connectionToolbar);

                return connectionToolbar;
            }
        }

        public bool IsVisible
        {
            get
            {
                return ConnectionStatusToolbar.Visibility == ViewStates.Visible;
            }
        }
    }
}