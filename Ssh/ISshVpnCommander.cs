using System.Collections.Generic;
using System.Threading.Tasks;

namespace VeeKee.Ssh
{
    public enum VpnStatus
    {
        Off = 0,
        Enabling = 1,
        Enabled = 2
    }

    interface ISshVpnCommander
    {
        Task<Dictionary<int, VpnStatus>> Status();

        Task<bool> EnableVpn(int vpnIndex);

        Task<bool> DisableVpn(int vpnIndex);
    }
}