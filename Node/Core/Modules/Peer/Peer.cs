using System;
using WatsonTcp;
using System.IO;
using System.Text;
using ADL.Network;
using ADL.Cryptography.SSL;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using WatsonTcp;

namespace ADL.Node.Core.Modules.Peer
{
    public class Peer : IPeer
    {
        private static bool _Daemon { get; set; }
        private static List<string> _Clients { get; set; }
        private static DirectoryInfo _dataDir { get; set; }
        private static WatsonTcpSslServer _Server { get; set; }
        private static ISslSettings _SslSettings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task StartPeer(ISslSettings sslSettings, string dataDir)
        {
            _SslSettings = sslSettings;
            _dataDir = new DirectoryInfo(dataDir);
            var task = Task.Factory.StartNew(StartTcpServer);
            task.ConfigureAwait(false);
            return task;
        }
          
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool StartTcpServer()
        {
            var serverIp = Ip.GetPublicIP();
            var serverPort = 8989;

            using (_Server = new WatsonTcpSslServer("127.0.0.1", serverPort, _dataDir+_SslSettings.PfxFileName, _dataDir+_SslSettings.SslCertPassword, true, false, ClientConnected, ClientDisconnected, MessageReceived, true))
            {
                _Daemon = true;
                while (_Daemon)
                {
                    _Clients = _Server.ListClients();
                    if (_Clients != null && _Clients.Count > 0)
                    {
                        Console.WriteLine("Clients");
                        foreach (string curr in _Clients) Console.WriteLine("  " + curr); 
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool StopPeer()
        {
            _Daemon = false;
            return _Daemon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipPort"></param>
        /// <returns></returns>
        private bool ClientConnected(string ipPort)
        {
            Console.WriteLine("Client connected: " + ipPort);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipPort"></param>
        /// <returns></returns>
        private bool ClientDisconnected(string ipPort)
        {
            Console.WriteLine("Client disconnected: " + ipPort);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipPort"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool MessageReceived(string ipPort, byte[] data)
        {
            string msg = "";
            if (data != null && data.Length > 0)
            {
                msg = Encoding.UTF8.GetString(data);
            }
            Console.WriteLine("Message received from " + ipPort + ": " + msg);
            return true;
        }
    }
}