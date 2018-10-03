using A = Android;
using Android.App;
using Android.OS;
using Android.Preferences;
using Android.Views;
using System;

namespace VeeKee.Android
{
    [Activity(Label = "Settings", Icon = "@drawable/settings")]
    public class AppPreferencesActivity : PreferenceActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            FragmentManager.BeginTransaction().Replace(A.Resource.Id.Content, new AppPreferencesFragment()).Commit();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case A.Resource.Id.Home:
                    Finish();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }            
        }
    }

    public class AppPreferencesFragment : PreferenceFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Add preferences from the preferences xml file
            AddPreferencesFromResource(Resource.Xml.Preferences);

            // Add Flag choice preferences
            var preferenceScreen = this.PreferenceScreen;

            for (int i=1; i< this.Resources.GetInteger(Resource.Integer.VpnItemCount) + 1; i++)
            {
                var vpnClientName = string.Format(this.Resources.GetString(Resource.String.VpnNamePreferenceFormat), i);
                var vpnClientFlagKey = String.Format(this.Resources.GetString(Resource.String.VpnFlagKeyPreferenceFormat), i);

                var namePreference = new EditTextPreference(preferenceScreen.Context);
                namePreference.Title = vpnClientName;
                namePreference.Key = vpnClientName;
                namePreference.Persistent = true;
                preferenceScreen.AddPreference(namePreference);

                var flagPreference = new ListPreference(preferenceScreen.Context);
                flagPreference.SetEntries(Resource.Array.flag_titles);
                flagPreference.SetEntryValues(Resource.Array.flag_image_names);
                flagPreference.Title = vpnClientName;
                flagPreference.Key = vpnClientFlagKey;
                flagPreference.Persistent = true;
                preferenceScreen.AddPreference(flagPreference);
            }
        }
    }
}