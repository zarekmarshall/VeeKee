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