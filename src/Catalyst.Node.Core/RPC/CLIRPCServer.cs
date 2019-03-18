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
    public class CLIRPCServer : ICLIRPCServer, IDisposable
    {
        private static string CatalystSubfolder => ".Catalyst";

        private readonly ILogger _logger;

        private readonly X509Certificate2 _certificate;

        private readonly ICLIRPCServerSettings _settings;

        private IChannel _serverChannel;

        private MultithreadEventLoopGroup _bossGroup;

        private MultithreadEventLoopGroup _workerGroup;

        public ICLIRPCServerSettings Settings
        {
            get { return _settings; }
        }

        public CLIRPCServer(ICLIRPCServerSettings settings, ILogger logger, ICertificateStore certificateStore)
        {
            _logger = logger;
            _settings = settings;

            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var catalystHomeDirectory = Path.Combine(homeDirectory, CatalystSubfolder);

            if(_settings.isSSL)
            {
                _certificate = certificateStore.ReadOrCreateCertificateFile(settings.CertFileName, settings.SslCertPassword);
            }

            Task.WaitAll(RunServerAsync());
        }

        public async Task RunServerAsync()
        {
            _logger.Information("CLI Server Started ...");

            //Helper.SetConsoleLogger();

            //Created a multithreaded event loop to handle I/O operation. 
            //To implement a server-side application two EventLoopGroup are needed. 
            //First EventLoopGroup,'boss', accepts an incoming connection.
            _bossGroup = new MultithreadEventLoopGroup();
            //second EventLoopGroup, 'worker', handles the traffic of the accepted connection once the boss accepts the connection and registers the accepted connection to the worker. 
            _workerGroup = new MultithreadEventLoopGroup();


            //Create a string encoder instance
            var STRING_ENCODER = new StringEncoder();

            //Create a string decoder instance
            var STRING_DECODER = new StringDecoder();

            //Create an instance of the Server Handler class
            var SERVER_HANDLER = new CLIRPCServerHandler();

            try
            {
                //Create an instance of the ServerBootStrap which sets up a server
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (_certificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(_certificate));
                        }

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(STRING_ENCODER, STRING_DECODER, SERVER_HANDLER);
                    }));

                _serverChannel = await bootstrap.BindAsync(_settings.Port);
            }
            catch(Exception e)
            {
                _logger.Information(e.InnerException.Message);
            }
        }

        /*Implementing IDisposable */
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("CLI RPC service is closing");

                try
                {
                   _serverChannel.CloseAsync(); 
                }
                finally
                {
                    Task.WaitAll(_bossGroup.ShutdownGracefullyAsync(), _workerGroup.ShutdownGracefullyAsync());
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        /****************************/
    }
}