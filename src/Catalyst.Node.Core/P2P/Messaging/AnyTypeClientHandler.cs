﻿/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Catalyst.Node.Common.Helpers.Util;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging {
    public class AnyTypeClientHandler : SimpleChannelInboundHandler<Any>, IMessageStreamer<Any>, IDisposable
    { 
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        public IObservable<Any> MessageStream => _messageSubject.AsObservable();
        private readonly BehaviorSubject<Any> _messageSubject = new BehaviorSubject<Any>(NullObjects.Any);
        
        protected override void ChannelRead0(IChannelHandlerContext context, Any message)
        {
            Logger.Debug(JsonConvert.SerializeObject(message) + Environment.NewLine);
            _messageSubject.OnNext(message);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in P2P client");
            context.CloseAsync().ContinueWith(_ => _messageSubject.OnCompleted());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageSubject?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}