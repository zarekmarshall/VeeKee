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

namespace VeeKee.Ssh
{
    public abstract class SshCommander
    {
        public RouterConnectionResult Connection { get; set; }

        public SshCommander()
        {
            this.Connection = new RouterConnectionResult();
        }
    }
}