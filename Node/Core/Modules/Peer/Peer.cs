using System;
using WatsonTcp;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ADL.Node.Core.Modules.Peer
{
    public static class Peer
    {
        private static bool _Daemon { get; set; }
        private static List<string> _Clients { get; set; }
        private static DirectoryInfo _dataDir { get; set; }
        private static ISslSettings _SslSettings { get; set; }
        private static WatsonTcpSslServer _Server { get; set; }
        private static IPeerSettings _PeerSettings { get; set; }
        static WatsonTcpClient c;

        static int serverPort = 8000;
        static int clientThreads = 128;
        static int numIterations = 10000;
        static Random rng;
        static byte[] data;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static async Task StartPeer(IPeerSettings peerSettings, ISslSettings sslSettings, string dataDir)
        {
            _SslSettings = sslSettings;
            _PeerSettings = peerSettings;
            _dataDir = new DirectoryInfo(dataDir);
            var task = Task.Factory.StartNew(StartTcpServer);
            task.ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static void StartTcpServer()
        {
#if DEBUG
            Console.WriteLine(_dataDir+"/"+_SslSettings.PfxFileName);
            Console.WriteLine(_SslSettings.SslCertPassword);
            Console.WriteLine(_PeerSettings.BindAddress);
            Console.WriteLine(_PeerSettings.Port);
#endif
            Console.WriteLine("123");
            try
            {
                WatsonTcpSslServer server = new WatsonTcpSslServer("127.0.0.1", 45521, _dataDir+"/"+_SslSettings.PfxFileName, _SslSettings.SslCertPassword, true, false, ClientConnected, ClientDisconnected, MessageReceived, true);
                         using (server)
            {
                Console.WriteLine("456");
                bool runForever = true;
                while (runForever)
                {
                    Console.Write("Command [? for help]: ");
                    string userInput = Console.ReadLine();

                    List<string> clients;
                    string ipPort;

                    if (String.IsNullOrEmpty(userInput))
                    {
                        continue;
                    }

                    switch (userInput)
                    {
                        case "?":
                            Console.WriteLine("Available commands:");
                            Console.WriteLine("  ?        help (this menu)");
                            Console.WriteLine("  q        quit");
                            Console.WriteLine("  cls      clear screen");
                            Console.WriteLine("  list     list clients");
                            Console.WriteLine("  send     send message to client");
                            Console.WriteLine("  remove   disconnect client");
                            break;

                        case "q":
                            runForever = false;
                            break;

                        case "cls":
                            Console.Clear();
                            break;

                        case "list":
                            clients = server.ListClients();
                            if (clients != null && clients.Count > 0)
                            {
                                Console.WriteLine("Clients");
                                foreach (string curr in clients)
                                {
                                    Console.WriteLine("  " + curr);
                                }
                            }
                            else
                            {
                                Console.WriteLine("None");
                            }
                            break;

                        case "send":
                            Console.Write("IP:Port: ");
                            ipPort = Console.ReadLine();
                            Console.Write("Data: ");
                            userInput = Console.ReadLine();
                            if (String.IsNullOrEmpty(userInput))
                            {
                                break;
                            }

                            server.Send(ipPort, Encoding.UTF8.GetBytes(userInput));
                            break;

                        case "remove":
                            Console.Write("IP:Port: ");
                            ipPort = Console.ReadLine();
                            server.DisconnectClient(ipPort);
                            break;

                        default:
                            break;
                    }
                }
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
 
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool StopPeer()
        {
            _Daemon = false;
            return _Daemon;
        }
    }

    internal class TcpServer
    {
        
    }
}
