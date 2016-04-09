using System;
using Renci.SshNet;
using System.Threading.Tasks;
using Renci.SshNet.Common;

namespace VeeKee.Ssh.SshNet
{
    public class SshNetCommander : ISshCommander, IDisposable
    {
        private SshClient _client;

        public SshNetCommander(string ipAddress, string userName, string password, int port) : this(new ConnectionInfo(ipAddress, userName, new AuthenticationMethod[] { new PasswordAuthenticationMethod(userName, password) }))
        {
        }

        public SshNetCommander(ConnectionInfo connectionInfo)
        { 
            _client = new SshClient(connectionInfo);
        }

        public async Task<ConnectionResult> Connect()
        {
            var status = new ConnectionResult();
            try
            {
                await Task.Run(() => _client.Connect());
            }
            catch (SshAuthenticationException ex)
            {
                status.Status = ConnectionStatus.AuthorizationError;
                status.Error = ex;
            }
            catch (Exception ex)
            {
                // Throw any unhandled exceptions
                status.Error = ex;
            }
            finally
            {
                if (status.Error == null)
                {
                    status.Status = ConnectionStatus.Connected;
                }
            }

            return status;
        }

        public async Task<string> SendCommand(string command)
        {
            string result = string.Empty;

            using (var sshCommand = _client.CreateCommand(command))
            {
                result = await Task<string>.Factory.FromAsync(sshCommand.BeginExecute(), sshCommand.EndExecute);
            }

            return result;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                if (_client.IsConnected)
                {
                    _client.Disconnect();
                }

                _client.Dispose();
            }
        }
    }
}