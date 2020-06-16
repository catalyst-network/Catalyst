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
using System.Reflection;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Protocol.Peer
{
    //public partial class PeerId
    //{
    //    private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

    //    public IPAddress IpAddress => GenerateIpFromByteString(Ip);
    //    public IPEndPoint IpEndPoint => new IPEndPoint(IpAddress, (int) Port);

    //    //Add support for IPv6
    //    private IPAddress GenerateIpFromByteString(ByteString ipAddressByteString)
    //    {
    //        var ipv4Buffer = new byte[4];
    //        var ipAddressBytes = ipAddressByteString.ToByteArray();
    //        Buffer.BlockCopy(ipAddressBytes, 12, ipv4Buffer, 0, 4);
    //        return new IPAddress(ipAddressBytes).MapToIPv4();
    //    }

    //    public bool IsValid()
    //    {
    //        if (PublicKey == null || PublicKey.IsEmpty)
    //        {
    //            Logger.Debug("{field} cannot be null or empty", nameof(PublicKey));
    //            return false;
    //        }

    //        if (Port > ushort.MaxValue)
    //        {
    //            Logger.Debug("{field} should have a value between 0 and {maxValue}", nameof(Port), ushort.MaxValue);
    //            return false;
    //        }

    //        return true;
    //    }
    //}
}
