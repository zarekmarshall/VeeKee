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
    [Activity(Label = "Settings", Icon = "@drawable/settings")]
    public class AppPreferencesActivity : PreferenceActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set up the Action Bar
            ActionBar.DisplayOptions = ActionBarDisplayOptions.ShowHome | ActionBarDisplayOptions.HomeAsUp | ActionBarDisplayOptions.ShowTitle;

            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.Content, new AppPreferencesFragment()).Commit();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
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
                var vpnClientName = string.Format(this.Resources.GetString(Resource.String.VpnFlagPreferenceFormat), i);
                var vpnClientFlagKey = String.Format(this.Resources.GetString(Resource.String.VpnFlagKeyPreferenceFormat), i);

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