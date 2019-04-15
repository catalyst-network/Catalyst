#region LICENSE
/**
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
#endregion

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Autofac;

using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;

using Microsoft.Extensions.Configuration;
using Multiformats.Base;
using Nethereum.RLP;
using NSec.Cryptography;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class VerifyMessageRequestHandlerTest : ConfigFileBasedTest, IDisposable
    {
        private const int MaxWaitInMs = 1000;
        private readonly IConfigurationRoot _config;

        private readonly ICertificateStore _certificateStore;
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;

        private readonly IKeySigner _keySigner;

        public VerifyMessageRequestHandlerTest(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build(), CurrentTestName);

            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();

            _scope = container.BeginLifetimeScope(CurrentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory
               .AddProvider(new SerilogLoggerProvider(_logger));

            _certificateStore = container.Resolve<ICertificateStore>();
            _keySigner = container.Resolve<IKeySigner>();
        }
        
        [Fact]
        public void RpcServer_Can_Handle_VerifyMessageRequest()
        {
            //Substitute for the context and the channel
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            
            fakeContext.Channel.Returns(fakeChannel);

            //Matching the SignMessageRequestHandler
            //sign a message to get the signature and the public key
            const string message = "Hello Catalyst";
            var privateKey = _keySigner.CryptoContext.GeneratePrivateKey();
            var signature = _keySigner.CryptoContext.Sign(privateKey, Encoding.UTF8.GetBytes(message));
            var publicKey = _keySigner.CryptoContext.GetPublicKey(privateKey).GetNSecFormatPublicKey()
               .Export(KeyBlobFormat.PkixPublicKey);

            
            var encodedSignature = Multibase.Encode(MultibaseEncoding.Base64, signature);
            var encodedPublicKey = Multibase.Encode(MultibaseEncoding.Base58Btc, publicKey);
            
            //create a verify message request and populate with the data returned from the signed message
            var request =new VerifyMessageRequest()
            {
                Message = message.ToUtf8ByteString(),
                PublicKey = encodedPublicKey.ToUtf8ByteString(),
                Signature = encodedSignature.ToUtf8ByteString()
            }.ToAny();
            
            //create a channel using the mock context and request
            var channeledAny = new ChanneledAny(fakeContext, request);
            
            //convert the channel created into an IObservable 
            //the .ToObervable() Converts the array to an observable sequence which means we can use it as a message
            //stream for the handler
            //The DelaySubscription() is required otherwise the constructor of the base class will call the
            //HandleMessage before the VerifyMessageRequestHandler constructor is executed. This happens because
            //the base class will find a message in the stream. As a result the _keySigner object
            //will have not been instantiated before calling the HandleMessage.
            //This case will not happen in realtime but it is caused by the test environment.
            var signRequest = new [] {channeledAny}.ToObservable()
               .DelaySubscription(TimeSpan.FromMilliseconds(50));
            
            //pass the created observable sequence as the message stream to the VerifyMessageRequestHandler
            //Calling the constructor will call the 
            var handler = new VerifyMessageRequestHandler(signRequest, _logger, _keySigner);
            
            //Another time delay is required so that the call to HandleMessage inside the VerifyMessageRequestHandler
            //is finished before we assert for the Received calls in the following statements.
            Thread.Sleep(500);

            fakeContext.Channel.ReceivedWithAnyArgs(1).WriteAndFlushAsync(new VerifyMessageResponse().ToAny());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) {return;}
            _scope?.Dispose();
        }
    }
}
