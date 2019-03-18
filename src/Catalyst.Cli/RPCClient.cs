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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Catalyst.Node.Common.Interfaces;

using DotNetty.Codecs;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using Serilog;

namespace Catalyst.Cli
{
    /// <summary>
    /// This class provides a command line interface (CLI) application to connect to Catalyst Node.
    /// Through the CLI the node operator will be able to connect to any number of running nodes and run commands. 
    /// </summary>
    public class RPCClient : IRPCClient, IDisposable
    {
        private MultithreadEventLoopGroup _group;
        
        //private readonly ILogger _logger;

        private readonly ICLIRPClientSettings _settings;
        
        private readonly X509Certificate2 _certificate;
        
        private static string CatalystSubfolder => ".Catalyst";
        
        /// <summary>
        /// Intialize a new instance of RPClient by doing the following:
        /// 1- Get the settings from the config file
        /// 2- Create/Read the SSL Certificate
        /// 3- Start the client
        /// </summary>
        /// <param name="settings">an object of ClientSettings which reads the settings from config file section RPCClient</param>
        /// <param name="logger">a logger instance</param>
        /// <param name="certificateStore">certification store object to create/read the SSL certificate</param>
        public RPCClient(ICLIRPClientSettings settings, ILogger logger, ICertificateStore certificateStore)
        {
            //_logger = logger;
            _settings = settings;

            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var catalystHomeDirectory = Path.Combine(homeDirectory, CatalystSubfolder);

            _certificate = new X509Certificate2(settings.CertFileName, settings.SslCertPassword);

            Task.WaitAll(RunClientAsync());
        }
        
        public async Task RunClientAsync()
        {
            Helper.SetConsoleLogger();

            //Create the event loop group
            _group = new MultithreadEventLoopGroup();

            /* SSL Certificate */
            X509Certificate2 cert = null;
            string targetHost = null;
            if (_settings.isSSL)
            {
                cert = new X509Certificate2(_settings.CertFileName, _settings.SslCertPassword);
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }
            /*******************/

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                   .Group(_group)
                   .Channel<TcpSocketChannel>()
                   .Option(ChannelOption.TcpNodelay, true)
                   .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (cert != null)
                        {
                            pipeline.AddLast(new TlsHandler(
                                stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true),
                                new ClientTlsSettings(targetHost)));
                        }

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(new StringEncoder(), new StringDecoder(), new RPClientHandler());
                    }));

                IChannel bootstrapChannel =
                    await bootstrap.ConnectAsync(new IPEndPoint(_settings.ServerAddress, _settings.Port));

                for (;;)
                {
                    string line = Console.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    try
                    {
                        await bootstrapChannel.WriteAndFlushAsync(line + "\r\n");
                    }
                    catch { }

                    if (string.Equals(line, "bye", StringComparison.OrdinalIgnoreCase))
                    {
                        await bootstrapChannel.CloseAsync();
                        break;
                    }
                }

                await bootstrapChannel.CloseAsync();
            }
            catch (Exception e)
            {
                //_logger.Information(e.InnerException.Message);
                throw e;
            }
        }
        
        /*Implementing IDisposable */
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _group.ShutdownGracefullyAsync().Wait(1000);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        /****************************/
    }
}