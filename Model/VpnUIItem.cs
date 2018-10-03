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
using VeeKee.Shared.Models;

namespace VeeKee.Android.Model
{
    public class VpnUIItem : IVpnUIItem
    {
        public int FlagResourceId { get; set; }
        public string Name { get; set; }
        public VpnStatus Status { get; set; }

        public VpnUIItem(string name, int flagResourceId, VpnStatus status)
        {
            this.Name = name;
            this.FlagResourceId = flagResourceId;
            this.Status = status;
        }
    }
}