#region LICENSE

/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Cryptography.BulletProofs.Wrapper <https://github.com/catalyst-network/Rust.Cryptography.FFI.Wrapper>
 *
 * Catalyst.Cryptography.BulletProofs.Wrapper is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Cryptography.BulletProofs.Wrapper is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Cryptography.BulletProofs.Wrapper If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Reflection;
using System.Resources;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Exceptions;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs
{
    internal static class Error
    {
        private static ResourceManager _sResourceManager;

        private static ResourceManager ResourceManager =>
            _sResourceManager ?? (_sResourceManager =
                new ResourceManager(typeof(Error).FullName,
                    typeof(Error).GetTypeInfo().Assembly));

        internal static void ThrowErrorFromErrorCode(string errorCode)
        {
            switch (errorCode)
            {
                case "101":
                    ThrowSignatureException();
                    break;
                default:
                    ThrowUnknownException();
                    break;
            }
        }

        private static string ErrorFromErrorNameAdditionalInfo(string name)
        {
            string error = ErrorFromErrorName(name);
            string additionalInfo = FFI.GetLastError();
            return error + " - " + additionalInfo + ".";
        }

        private static string ErrorFromErrorName(string name)
        {
            return ResourceManager.GetString(name);   
        }

        private static void ThrowSignatureException()
        {
            const string errorName = "Signature_Exception";
            var error = ErrorFromErrorNameAdditionalInfo(errorName);
            throw new SignatureException(error);
        }

        private static void ThrowUnknownException()
        {
            const string errorName = "Unknown_Exception";
            var error = ErrorFromErrorNameAdditionalInfo(errorName);
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
            var error = ErrorFromErrorName(errorName);
            throw new ArgumentException(string.Format(error, requiredLength));
        }        
    }
}
