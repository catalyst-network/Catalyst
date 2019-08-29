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
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.IO.Handlers
{
    public abstract class InboundChannelHandlerBase<T> : ChannelHandlerAdapter
    {
        private readonly bool _autoRelease;
        protected readonly ILogger Logger;

        protected InboundChannelHandlerBase(ILogger logger) : this(true)
        {
            Logger = logger;
        }
        
        private InboundChannelHandlerBase(bool autoRelease)
        {
            _autoRelease = autoRelease;
        }
        
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            var release = true;
            try
            {
                if (msg is T msg1)
                {
                    ChannelRead0(ctx, msg1);
                }
                else
                {
                    release = false;
                    ctx.FireChannelRead(msg);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            finally
            {
                if (_autoRelease && release)
                {
                    ReferenceCountUtil.Release(msg);   
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        protected abstract void ChannelRead0(IChannelHandlerContext ctx, T msg);
    }
}
