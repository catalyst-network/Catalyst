#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Serilog;

namespace Catalyst.Protocol.Cryptography
{
    public partial class Signature
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Checks if the signature seems valid.
        /// </summary>
        /// <remarks>
        /// The validation, using a public key, of the actual signature against a content it signs,
        /// is outside the scope of this method. In the Core implementation of the protocol, this
        /// is performed in the Cryptography module.
        /// </remarks>
        /// <param name="expectedSignatureType">If provided, the signature type will be checked
        /// against the expected type. Otherwise, we simply check the type is not unknown.</param>
        public bool IsValid(SignatureType expectedSignatureType = SignatureType.Unknown)
        {
            if (RawBytes == null || RawBytes.IsEmpty)
            {
                Logger.Debug("{field} cannot be null or empty", nameof(RawBytes));
                return false;
            }

            return SigningContext.IsValid(expectedSignatureType);
        }
    }
}
