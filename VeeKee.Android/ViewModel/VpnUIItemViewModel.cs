using Android.Content.Res;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeeKee.Android.Model;
using VeeKee.Shared.CountryCodeLookup;
using VeeKee.Shared.Models;
using VeeKee.Shared.Ssh;

namespace VeeKee.Android.ViewModel
{
    public class VpnUIItemViewModel
    {
        public Dictionary<int, VpnUIItem> VpnUIItems { get; set; }

        private string _packageName;
        private Resources _resources;

        public VpnUIItemViewModel(string packageName, Resources resources)
        {
            VpnUIItems = new Dictionary<int, VpnUIItem>();
            this._packageName = packageName;
            this._resources = resources;
        }

        public void LoadVpnUIItemsFromPreferences(VeeKeePreferences preferences)
        {
            for (int i = 1; i < this._resources.GetInteger(Resource.Integer.VpnItemCount) + 1; i++)
            {
                var vpnItemName = string.Format(this._resources.GetString(Resource.String.VpnClientFormat), i);

                VpnUIItems.Add(i, new VpnUIItem(preferences.GetVpnName(i), GetFlagResourceId(preferences.GetVpnFlag(i), this._packageName, this._resources), VpnStatus.Off));
            }
        }

        public void SaveVpnUIItemsPreferences(VeeKeePreferences preferences)
        {
            for (int i = 1; i < VpnUIItems.Count+1; i++)
            {
                var vpnUIItem = VpnUIItems[i];
                preferences.SetVpnName(i, vpnUIItem.Name);
                preferences.SetVpnFlag(i, vpnUIItem.FlagResourceId.ToString());
            }
        }

        public async Task<bool> UpdateVpnUIItemStatus(AsusSshVpnService asusCommander)
        {
            if (asusCommander.Connection.IsConnected)
            {
                var vpnStatus = await asusCommander.Status();

                foreach (var key in vpnStatus.Keys)
                {
                    VpnUIItems[key].Status = vpnStatus[key];
                }

                return true;
            }

            return false;
        }

        public async Task<bool> AutoConfigureFromRouterSettings(AsusSshVpnService asusCommander, VeeKeePreferences preferences)
        {
            Dictionary<int, VpnDetails> vpnDetails = null;

            var connected = await asusCommander.Connect();
            var routerStatus = asusCommander.Connection.Status;

            if (routerStatus == RouterConnectionStatus.Connected)
            {
                // Get a Dictionary of all the VPN details from the router settings
                vpnDetails = await asusCommander.Details();
            }
            
            for (int i = 1; i < VpnUIItems.Count+1; i++)
            {
                var vpnUIItem = VpnUIItems[i];

                vpnUIItem.Name = vpnDetails[i].Name;
            }

            // Lookup the country codes for each configured VPN
            foreach (int vpnIndex in vpnDetails.Keys)
            {
                var address = vpnDetails[vpnIndex].Address;

                if (address != string.Empty)
                {
                    string countryCode = await IPStackCountryLookupService.LookupAsync(vpnDetails[vpnIndex].Address, new CancellationTokenSource().Token);

                    if (countryCode != null)
                    {
                        int flagResourceId = GetFlagResourceId(countryCode, this._packageName, this._resources);
                        VpnUIItems[vpnIndex].FlagResourceId = flagResourceId;
                    }
                }
            }

            this.SaveVpnUIItemsPreferences(preferences);

            return true;
        }

        private int GetFlagResourceId(string flagName, string packageName, Resources resources)
        {
            int resourceId = resources.GetIdentifier(flagName.ToLower(), "drawable", packageName);

            if (resourceId == 0)
            {
                resourceId = Resource.Drawable.DEFAULTFLAG;
            }

            return resourceId;
        }
    }
}