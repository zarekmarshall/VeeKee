using System;
using Xamarin.Essentials;

namespace VeeKee.Android
{
    public class VeeKeePreferences
    {
        private const string FirstRunPreferenceKey = "FirstRun";
        private const string WifiNamePreferenceName = "WifiName";
        private const string RouterIpAddressPreferenceKey = "RouterIpAddress";
        private const string RouterUsernamePreferenceKey = "RouterUsername";
        private const string RouterPasswordPreferenceKey = "RouterPassword";
        private const string RouterPortPreferenceKey = "RouterSSHPort";

        private const string RouterIpAddressPreferenceDefault = "192.168.1.1";
        private const string RouterUsernamePreferenceDefault = "admin";
        private const int RouterPortPreferenceDefault = 22;
        private const string VpnNamePreferenceDefaultFormat = "Client {0}";

        public const string VpnNamePreferenceKeyFormat = "VPNClient_{0}_Name";
        public const string VpnFlagPreferenceKeyFormat = "VPNClient{0}_flag";

        public bool FirstRun
        {
            get
            {
                return Preferences.Get(FirstRunPreferenceKey, true);
            }
            set
            {
                Preferences.Set(FirstRunPreferenceKey, value);
            }
        }

        public string RouterIpAddress
        {
            get
            {
                return Preferences.Get(RouterIpAddressPreferenceKey, RouterIpAddressPreferenceDefault);
            }
            set
            {
                Preferences.Set(RouterIpAddressPreferenceKey, value);
            }
        }

        public string RouterUsername
        {
            get
            {
                return Preferences.Get(RouterUsernamePreferenceKey, RouterUsernamePreferenceDefault);
            }
            set
            {
                Preferences.Set(RouterUsernamePreferenceKey, value);
            }
        }

        public string RouterPassword
        {
            get
            {
                return Preferences.Get(RouterPasswordPreferenceKey, string.Empty);
            }
            set
            {
                Preferences.Set(RouterPasswordPreferenceKey, value);
            }
        }

        public string RouterPort
        {
            get
            {
                return Preferences.Get(RouterPortPreferenceKey, RouterPortPreferenceDefault.ToString());
            }
            set
            {
                Preferences.Set(RouterPortPreferenceKey, value);
            }
        }

        public string GetVpnName(int i)
        {
            var key = String.Format(VpnNamePreferenceKeyFormat, i);
            var defaultName = String.Format(VpnNamePreferenceDefaultFormat, i);
            return Preferences.Get(key, defaultName);
        }

        public void SetVpnName(int i, string name)
        {
            var key = String.Format(VpnNamePreferenceKeyFormat, i);
            Preferences.Set(key, name);
        }

        public string GetVpnFlag(int i)
        {
            var key = String.Format(VpnFlagPreferenceKeyFormat, i);
            return Preferences.Get(key, string.Empty);
        }

        public void SetVpnFlag(int i, string flag)
        {
            var key = String.Format(VpnFlagPreferenceKeyFormat, i);
            Preferences.Set(key, flag);
        }
    }
}