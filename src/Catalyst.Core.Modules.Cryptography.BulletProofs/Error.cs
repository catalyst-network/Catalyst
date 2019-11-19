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
using System.Resources;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Exceptions;
using Catalyst.Protocol.Cryptography;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs
{
    internal static class Error
    {
        private static ResourceManager _sResourceManager;

        private static ResourceManager ResourceManager =>
            _sResourceManager ?? (_sResourceManager =
                new ResourceManager(typeof(Error).FullName,
                    typeof(Error).GetTypeInfo().Assembly));

        internal static void ThrowErrorFromErrorCode(ErrorCode errorCode)
        {
            switch (errorCode)
            { 
                case ErrorCode.NoError:
                    break;
                case ErrorCode.InvalidSignature:
                case ErrorCode.InvalidPublicKey:
                case ErrorCode.InvalidPrivateKey:
                case ErrorCode.SignatureVerificationFailure:
                case ErrorCode.InvalidContextLength:
                    ThrowSignatureException(errorCode);
                    break;
                default:
                    ThrowUnknownException(errorCode);
                    break;
            }
        }

        private static string ErrorDescFromErrorCode(string name)
        {
            return ResourceManager.GetString(name);   
        }

        private static void ThrowSignatureException(ErrorCode errorCode)
        {
            string error = ErrorDescFromErrorCode(errorCode.ToString());
            throw new SignatureException(error);
        }

        private static void ThrowUnknownException(ErrorCode errorCode)
        {
            string error = ErrorDescFromErrorCode(errorCode.ToString());
            throw new UnknownException(error);
        }

        internal static void ThrowArgumentExceptionPrivateKeyLength(int requiredLength)
        {
            const string errorName = "Argument_Invalid_PrivateKey_Length";
            ThrowArgumentExceptionLength(errorName, requiredLength);
        }

        internal static void ThrowArgumentExceptionPublicKeyLength(int requiredLength)
        {
            const string errorName = "Argument_Invalid_PublicKey_Length";
            ThrowArgumentExceptionLength(errorName, requiredLength);
        } 

        internal static void ThrowArgumentExceptionSignatureLength(int requiredLength)
        {
            const string errorName = "Argument_Invalid_Signature_Length";
            ThrowArgumentExceptionLength(errorName, requiredLength);
        }

        private static void ThrowArgumentExceptionLength(string errorName, int requiredLength)
        {
            var error = ErrorDescFromErrorCode(errorName);
            throw new ArgumentException(string.Format(error, requiredLength));
        }        
    }
}
