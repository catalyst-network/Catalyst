using System;
using WatsonTcp;
using System.IO;
using System.Text;
using ADL.Network;
using ADL.Cryptography.SSL;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace ADL.Peer
{
    public static class Peer
    {
        private static bool Daemon { get; set; }
        private static List<string> Clients { get; set; }
        private static DirectoryInfo DataDir { get; set; }
        private static IPeerSettings PeerSettings { get; set; }

                /// <summary>
        /// PeerController constructor.
        /// </summary>
        public PeerService()
        {
            DateTime currentUTC = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a UTC DateTime obj
        /// </summary>
        /// <returns></returns>
        DateTime CurrentUTCTime()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static Task AsyncWrapper()
        {         
            var task = Task.Factory.StartNew(RunWatson);
            task.ConfigureAwait(false);
            return task;
        }
        
        private static bool RunWatson()
        {
            var serverIp = Ip.GetPublicIP();
            var serverPort = 8989;

//            using (Server = new WatsonTcpSslServer("127.0.0.1", serverPort, DataDir+SslSettings.PfxFileName, SslSettings.SslCertPassword, true, false, ClientConnected, ClientDisconnected, MessageReceived, true))
//            {
//                Daemon = true;
//                while (Daemon)
//                {
//                    Clients = Server.ListClients();
//                    if (Clients != null && Clients.Count > 0)
//                    {
//                        Console.WriteLine("Clients");
//                        foreach (string curr in Clients) Console.WriteLine("  " + curr); 
//                    }
//                }
//            }
            return true;
        }

        public static bool ShutDown()
        {
            Daemon = false;
            return Daemon;
        }

        private static bool ClientConnected(string ipPort)
        {
            Console.WriteLine("Client connected: " + ipPort);
            return true;
        }

        private static bool ClientDisconnected(string ipPort)
        {
            Console.WriteLine("Client disconnected: " + ipPort);
            return true;
        }

        private static bool MessageReceived(string ipPort, byte[] data)
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