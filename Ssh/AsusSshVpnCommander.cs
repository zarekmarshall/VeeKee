using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using VeeKee.Ssh.SshNet;

namespace VeeKee.Ssh
{
    public class AsusSshVpnCommander : SshNetCommander, ISshVpnCommander
    {
        private const int VpnClientCount = 5;

        // Vpn commands
        private const string EnableVpnClientCommandTemplate = "service start_vpnclient{0}";
        private const string DisableVpnClientCommandTemplate = "service stop_vpnclient{0}";
        private const string VpnClientStatusCommandTemplate = "nvram get vpn_client{0}_state";

        // Vpn status results
        private const int VpnClientStatusOff = 0;
        private const int VpnClientStatusEnabling = 1;
        private const int VpnClientStatusEnabled = 2;

        public AsusSshVpnCommander(string ipAddress, string userName, string password, int port, int connectionTimeoutSeconds) : base(ipAddress, userName, password, port, connectionTimeoutSeconds)
        {
        }

        public async Task<Dictionary<int, VpnStatus>> Status()
        {
            var vpnStatusList = new Dictionary<int, VpnStatus>();

            for (int i = 1; i < VpnClientCount + 1; i++)
            {
                var vpnStatusResult = await SendCommand(string.Format(VpnClientStatusCommandTemplate, i));
                vpnStatusList.Add(i, ParseVpnStatusResult(vpnStatusResult));
            }
            
            return vpnStatusList;
        }

        public async Task<bool> DisableVpn(int vpnIndex)
        {
            return await ToggleVpn(vpnIndex, false);
        }

        public async Task<bool> EnableVpn(int vpnIndex)
        {
            return await ToggleVpn(vpnIndex, true);
        }

        private async Task<bool> ToggleVpn(int vpnIndex, bool enabled)
        {
            string result = string.Empty;

            var disableCommands = new List<string>();

            // Construct commands for disabling all Vpns
            for (int i = 1; i < VpnClientCount + 1; i++)
            {
                var command = string.Format(DisableVpnClientCommandTemplate, i);
                disableCommands.Add(command);
            }

            // Run commands to disable all Vpns
            await Task.WhenAll(disableCommands.Select(c => SendCommand(c)));

            if (enabled)
            {
                // Run command to enable chosen Vpn
                var enableCommand = string.Format(EnableVpnClientCommandTemplate, vpnIndex);

                result = await SendCommand(enableCommand);
            }

            // TODO - do something with result
            return true;
        }

        private VpnStatus ParseVpnStatusResult(string result)
        {
            var status = VpnStatus.Off;
            int resultValue = int.Parse(result.Trim());

            switch (resultValue)
            {
                case VpnClientStatusOff:
                    status = VpnStatus.Off;
                    break;
                case VpnClientStatusEnabling:
                    status = VpnStatus.Enabling;
                    break;
                case VpnClientStatusEnabled:
                    status = VpnStatus.Enabled;
                    break;
            }

            return status;
        }
    }
}