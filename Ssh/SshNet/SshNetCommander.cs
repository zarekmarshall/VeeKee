using System;
using Renci.SshNet;
using System.Threading.Tasks;
using Renci.SshNet.Common;

namespace VeeKee.Ssh.SshNet
{
    public class SshNetCommander : SshCommander, ISshCommander, IDisposable
    {
        private SshClient _client;

        public SshNetCommander(string ipAddress, string userName, string password, int port) : this(new ConnectionInfo(ipAddress, userName, new AuthenticationMethod[] { new PasswordAuthenticationMethod(userName, password) }))
        {
        }

        public SshNetCommander(ConnectionInfo connectionInfo)
        { 
            _client = new SshClient(connectionInfo);
        }

        public async Task<bool> Connect()
        {
            this.Connection = new ConnectionResult();
            try
            {
                await Task.Run(() => _client.Connect());
            }
            catch (SshAuthenticationException ex)
            {
                this.Connection.Status = ConnectionStatus.AuthorizationError;
                this.Connection.Error = ex;
            }
            catch (Exception ex)
            {
                this.Connection.Error = ex;
            }
            finally
            {
                if (this.Connection.Error == null)
                {
                    this.Connection.Status = ConnectionStatus.Connected;
                }
            }

            return this.Connection.IsConnected;
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