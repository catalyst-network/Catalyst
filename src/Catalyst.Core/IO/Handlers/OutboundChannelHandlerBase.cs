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
using System.Reflection;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.IO.Handlers
{
    /// <summary>
    ///     OutboundChannel Handler is similar to Dot Netty's simple inbound channel handler, except it removes some redundant double cast operations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class OutboundChannelHandlerBase<T> : ChannelHandlerAdapter
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        ///     Does check to see if it can process the msg, if object is T thn it fires the inheritor WriteAsync0
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override Task WriteAsync(IChannelHandlerContext ctx, object msg)
        {
            Task writeTask = null;
            try
            {
                if (msg is T msg1)
                {
                    writeTask = WriteAsync0(ctx, msg1);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            
            return writeTask ?? ctx.WriteAsync(msg);
        }

        /// <summary>
        ///     Only fires if the msg if is the same as the classes generic T, should be implemented by inherited concrete classes.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected abstract Task WriteAsync0(IChannelHandlerContext ctx, T msg);
    }
}
