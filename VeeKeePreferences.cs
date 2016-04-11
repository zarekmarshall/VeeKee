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
using Android.Preferences;

namespace VeeKee
{
    public class VeeKeePreferences
    {
        private const string FirstRunPreferenceKey = "FirstRun";
        private const string WifiNamePreferenceName = "WifiName";
        private string _routerIpAddressPreferenceKey;
        private string _routerUsernamePreferenceKey;
        private string _routerPasswordPreferenceKey;
        private string _routerPortPreferenceKey;

        private const string RouterIpAddressPreferenceDefault = "192.168.1.1";
        private const string RouterUsernamePreferenceDefault = "admin";
        private const int RouterPortPreferenceDefault = 22;

        private ISharedPreferences _sharedPreferences;
        private ISharedPreferencesEditor _sharedPreferencesEditor;
        private Context _context;

        public VeeKeePreferences(Context context)
        {
            this._context = context;

            // Set up preference keys
            _routerIpAddressPreferenceKey = _context.GetString(Resource.String.RouterIpAddressPreferenceKey);
            _routerUsernamePreferenceKey = _context.GetString(Resource.String.RouterUsernamePreferenceKey);
            _routerPasswordPreferenceKey = _context.GetString(Resource.String.RouterPasswordPreferenceKey);
            _routerPortPreferenceKey = _context.GetString(Resource.String.RouterSshPortPreferenceKey);

            _sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this._context);
            _sharedPreferencesEditor = _sharedPreferences.Edit();
        }

        public void Save()
        {
            _sharedPreferencesEditor.Apply();
        }

        public bool FirstRun
        {
            get
            {
                return _sharedPreferences.GetBoolean(FirstRunPreferenceKey, true);
            }
            set
            {
                _sharedPreferencesEditor.PutBoolean(FirstRunPreferenceKey, value);
            }
        }

        public string WifiName
        {
            get
            {
                return _sharedPreferences.GetString(WifiNamePreferenceName, string.Empty);
            }
            set
            {
                _sharedPreferencesEditor.PutString(WifiNamePreferenceName, value);
            }
        }

        public string RouterIpAddress
        {
            get
            {
                return _sharedPreferences.GetString(_routerIpAddressPreferenceKey, string.Empty);
            }
            set
            {
                _sharedPreferencesEditor.PutString(_routerIpAddressPreferenceKey, value);
            }
        }

        public string RouterUsername
        {
            get
            {
                return _sharedPreferences.GetString(_routerUsernamePreferenceKey, string.Empty);
            }
            set
            {
                _sharedPreferencesEditor.PutString(_routerUsernamePreferenceKey, value);
            }
        }

        public string RouterPassword
        {
            get
            {
                return _sharedPreferences.GetString(_routerPasswordPreferenceKey, string.Empty);
            }
            set
            {
                _sharedPreferencesEditor.PutString(_routerPasswordPreferenceKey, value);
            }
        }

        public string RouterPort
        {
            get
            {
                return _sharedPreferences.GetString(_routerPortPreferenceKey, RouterPortPreferenceDefault.ToString());
            }
            set
            {
                _sharedPreferencesEditor.PutString(_routerPortPreferenceKey, value);
            }
        }

        public string GetVpnFlag(int i)
        {
            var key = String.Format(this._context.GetString(Resource.String.VpnFlagKeyPreferenceFormat), i);
            return _sharedPreferences.GetString(key, this._context.GetString(Resource.String.DefaultFlag));
        }
    }
}