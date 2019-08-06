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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using Polly;
using Polly.Retry;

namespace Catalyst.Common.IO.Transport.Bootstrapping
{
    public sealed class Bootstrap
        : DotNetty.Transport.Bootstrapping.Bootstrap,
            IServerBootstrap
    {
        private static TimeSpan DefaultRetryPolicy(int retryAttempt) => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt));

        private readonly AsyncRetryPolicy _exponentialBackOffRetryPolicy;

        public Bootstrap(int retryCount = 10) : this(retryCount, DefaultRetryPolicy)
        {
            AsyncRetryPolicy retryPolicy = Policy.Handle<SocketException>().WaitAndRetryAsync(retryCount, DefaultRetryPolicy);
        }

        public Bootstrap(int retryCount, Func<int, TimeSpan> retryPolicy)
        {
            _exponentialBackOffRetryPolicy = Policy.Handle<SocketException>()
               .WaitAndRetryAsync(retryCount, retryPolicy);
        }

        public new Task<IChannel> BindAsync(IPAddress ipAddress, int port)
        {
            return _exponentialBackOffRetryPolicy.ExecuteAsync(
                () => base.BindAsync(ipAddress, port)
            );
        }
    }
}
