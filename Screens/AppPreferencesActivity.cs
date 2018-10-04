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

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

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
                var namePreferenceTitle = string.Format(this.Resources.GetString(Resource.String.VpnNamePreferenceTitleFormat), i);
                var namePreference = new EditTextPreference(preferenceScreen.Context);
                namePreference.Title = namePreferenceTitle;
                namePreference.Key = String.Format(VeeKeePreferences.VpnNamePreferenceKeyFormat, i);
                namePreference.Persistent = true;
                preferenceScreen.AddPreference(namePreference);

                var flagPreferenceTitle = string.Format(this.Resources.GetString(Resource.String.VpnFlagKeyPreferenceTitleFormat), i);
                var flagPreference = new ListPreference(preferenceScreen.Context);
                flagPreference.SetEntries(Resource.Array.flag_titles);
                flagPreference.SetEntryValues(Resource.Array.flag_image_names);
                flagPreference.Title = flagPreferenceTitle;
                flagPreference.Key = String.Format(VeeKeePreferences.VpnFlagPreferenceKeyFormat, i); ;
                flagPreference.Persistent = true;
                preferenceScreen.AddPreference(flagPreference);
            }
        }
    }
}