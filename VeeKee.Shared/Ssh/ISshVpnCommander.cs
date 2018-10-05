using System.Collections.Generic;
using System.Threading.Tasks;
using VeeKee.Shared.Models;

namespace VeeKee.Shared.Ssh
{
    public class VpnDetails
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    interface ISshVpnCommander
    {
        Task<Dictionary<int, VpnStatus>> Status();

        Task<bool> EnableVpn(int vpnIndex);

        Task<bool> DisableVpn(int vpnIndex);
    }
}