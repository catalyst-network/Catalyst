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
using System.Threading.Tasks;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class OutboundChannelHandlerBase<T> : ChannelHandlerAdapter
    {
        private readonly ILogger _logger;
        private readonly bool _autoRelease;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        protected OutboundChannelHandlerBase(ILogger logger)
            : this(true)
        {
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoRelease"></param>
        private OutboundChannelHandlerBase(bool autoRelease)
        {
            _autoRelease = autoRelease;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override Task WriteAsync(IChannelHandlerContext ctx, object msg)
        {
            var flag = true;
            try
            {
                if (msg is T msg1)
                {
                    return WriteAsync0(ctx, msg1);
                }
                else
                {
                    flag = false;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                if (_autoRelease && flag)
                {
                    ReferenceCountUtil.Release(msg);   
                }
            }
            
            return ctx.WriteAsync(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected abstract Task WriteAsync0(IChannelHandlerContext ctx, T msg);
    }
}
