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
using System.Globalization;
using System.Net;
using Dawn;

namespace Catalyst.Core.Network
{
    /// <summary>
    /// </summary>
    public static class EndpointBuilder
    {
        /// <summary>
        ///     Handles IPv4 and IPv6 notation. of an ip:port string
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static IPEndPoint BuildNewEndPoint(string endPoint)
        {
            var ep = endPoint.Split(':');
            if (ep.Length < 2)
            {
                throw new FormatException("Invalid endpoint format");
            }

            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip address");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip address");
                }
            }

            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
            {
                throw new FormatException("Invalid port");
            }

            return BuildNewEndPoint(ip, port);
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint BuildNewEndPoint(string ip, int port)
        {
            Guard.Argument(ip, nameof(ip)).NotNull();
            Guard.Argument(port, nameof(port)).Min(1025).Max(65535);

            var validatedIp = Ip.ValidateIp(ip);

            return BuildNewEndPoint(validatedIp, port);
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint BuildNewEndPoint(IPAddress ip, int port)
        {
            Guard.Argument(ip, nameof(ip)).NotNull();
            Guard.Argument(port, nameof(port)).Min(1025).Max(65535);
            return new IPEndPoint(ip, port);
        }
    }
}
