using System;
using Renci.SshNet;
using System.Threading.Tasks;
using Renci.SshNet.Common;
using System.Net.Sockets;

namespace VeeKee.Ssh.SshNet
{
    public class SshNetCommander : SshCommander, ISshCommander, IDisposable
    {
        private SshClient _client;

        public SshNetCommander(string ipAddress, string userName, string password, int port, int connectionTimeoutSeconds) : this(new ConnectionInfo(ipAddress, port, userName, new AuthenticationMethod[] { new PasswordAuthenticationMethod(userName, password) }))
        {
            _client.ConnectionInfo.Timeout = new TimeSpan(0, 0, connectionTimeoutSeconds);
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
            catch (Exception ex)
            {
                if (ex is SshAuthenticationException)
                {
                    this.Connection.Status = ConnectionStatus.AuthorizationError;
                }
                else if (ex is SshOperationTimeoutException)
                {
                    this.Connection.Status = ConnectionStatus.ConnectionTimeoutError;
                }
                else if (ex is SocketException)
                {
                    this.Connection.Status = ConnectionStatus.NetworkError;
                }

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