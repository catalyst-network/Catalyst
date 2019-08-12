using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Cryptography;
using Catalyst.Common.FileSystem;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Keystore;
using Catalyst.Common.Modules.KeySigner;
using Catalyst.Common.P2P;
using Catalyst.Common.Registry;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.Shell;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Node.Rpc.Client;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Node.Rpc.Client.IO.Transport.Channels;
using Catalyst.Protocol.Rpc.Node;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Simulator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Catalyst Network Simulator");

            var simulator = new Simulator();
            await simulator.Simulate();

            //while (true)
            //{
            //    Thread.Sleep(100);
            //}
        }
    }
}
