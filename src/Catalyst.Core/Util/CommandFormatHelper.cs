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

using System.Text;
using Catalyst.Protocol.Common;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Util
{
    /// <summary>
    /// Formats command response data into readable text output
    /// </summary>
    public static class CommandFormatHelper
    {
        /// <summary>
        /// Format the repeated peer info response
        /// </summary>
        /// <param name="repeatedPeerInfo">The repeuated peer info to write out</param>
        /// <returns>String of the formatted response</returns>
        public static string FormatRepeatedPeerInfoResponse(RepeatedField<PeerInfo> repeatedPeerInfo)
        {
            var stringBuilder = new StringBuilder();
            foreach (var peerInfo in repeatedPeerInfo)
            {
                stringBuilder.AppendLine($"BlackListed={peerInfo.BlackListed}");
                stringBuilder.AppendLine($"Reputation={peerInfo.Reputation}");
                stringBuilder.AppendLine($"IsAwolPeer={peerInfo.IsAwolPeer}");
                stringBuilder.AppendLine($"InactiveFor={peerInfo.InactiveFor.ToTimeSpan().ToString("c")}");
                stringBuilder.AppendLine($"LastSeen={peerInfo.LastSeen.ToDateTime().ToString("MM/dd/yyyy HH:mm:ss")}");

                //Modified can be optional, the null check is incase it is.
                if (peerInfo.Modified != null)
                {
                    stringBuilder.AppendLine($"Modified={peerInfo.Modified.ToDateTime().ToString("MM/dd/yyyy HH:mm:ss")}");
                }

                stringBuilder.AppendLine($"Created={peerInfo.Created.ToDateTime().ToString("MM/dd/yyyy HH:mm:ss")}");
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }
    }
}
