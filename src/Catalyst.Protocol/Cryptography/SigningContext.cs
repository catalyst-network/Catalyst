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

using System.Reflection;
using Catalyst.Protocol.Network;
using Serilog;

namespace Catalyst.Protocol.Cryptography
{
    public partial class SigningContext
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Checks if the signing context is usable.
        /// </summary>
        /// <param name="expectedSignatureType">If provided, the signature type will be checked
        /// against the expected type. Otherwise, we simply check the type is not unknown.</param>
        public bool IsValid(SignatureType expectedSignatureType = SignatureType.Unknown)
        {
            if (NetworkType == NetworkType.Unknown)
            {
                Logger.Debug("{field} cannot be {value}", nameof(NetworkType), NetworkType.Unknown);
                return false;
            }

            if (SignatureType == SignatureType.Unknown)
            {
                Log.Debug("{field} cannot be {value}", nameof(SignatureType), SignatureType.Unknown);
                return false;
            }

            if (expectedSignatureType != SignatureType.Unknown && SignatureType != expectedSignatureType)
            {
                Log.Debug("{field} is expected to have value {value}", nameof(SignatureType), expectedSignatureType);
            }

            return true;
        }
    }
}
