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
    [Activity(Label = "AppPreferencesActivity")]
    public class AppPreferencesActivity : PreferenceActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.Content, new AppPreferencesFragment()).Commit();
        }
    }
    
    public class AppPreferencesFragment : PreferenceFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Xml.Preferences);
        }
    }
}