using System.Threading.Tasks;

namespace VeeKee.Shared.Ssh
{
    interface ISshCommander
    {
        Task<bool> Connect();

        Task<string> SendCommand(string command);
    }
}