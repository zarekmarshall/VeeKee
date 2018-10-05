namespace VeeKee.Shared.Models
{
    public enum VpnStatus
    {
        Off = 0,
        Enabling = 1,
        Enabled = 2
    }

    public interface IVpnUIItem
    {
        string Name { get; set; }
        VpnStatus Status { get; set; }
    }
}
