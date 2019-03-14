/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
 *
 * Catalyst.Node is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Node is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using Catalyst.Node.Common.Interfaces;

using Serilog;

namespace Catalyst.Node.Core.RPC
{
    public class CLIRPCServer : ICLIRPCServer
    {
        private static string CatalystSubfolder => ".Catalyst";

        private readonly ILogger _logger;

        public CLIRPCServer(ILogger logger)
        {
            Console.WriteLine("Server started ...");

            _logger = logger;
        }

        public async Task RunServerAsync()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var catalystHomeDirectory = Path.Combine(homeDirectory, CatalystSubfolder);

            //Helper.SetConsoleLogger();

            //Created a multithreaded event loop to handle I/O operation. 
            //To implement a server-side application two EventLoopGroup are needed. 
            //First EventLoopGroup,'boss', accepts an incoming connection.
            var bossGroup = new MultithreadEventLoopGroup();
            //second EventLoopGroup, 'worker', handles the traffic of the accepted connection once the boss accepts the connection and registers the accepted connection to the worker. 
            var workerGroup = new MultithreadEventLoopGroup();


            //Create a string encoder instance
            var STRING_ENCODER = new StringEncoder();

            //Create a string decoder instance
            var STRING_DECODER = new StringDecoder();

            //Create an instance of the Server Handler class
            var SERVER_HANDLER = new CLIRPCServerHandler();

            //Check for SSL Certificates
            X509Certificate2 tlsCertificate = null;
            if (ServerSettings.IsSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(catalystHomeDirectory, "public.pem"), "test");
            }
            try
            {
                //Create an instance of the ServerBootStrap which sets up a server
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        }

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(STRING_ENCODER, STRING_DECODER, SERVER_HANDLER);
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(ServerSettings.Port);

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }
    }
}