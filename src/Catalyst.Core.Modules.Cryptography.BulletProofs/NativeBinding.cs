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

using System.Runtime.InteropServices;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Types;
using Catalyst.Protocol.Cryptography;
using Google.Protobuf.WellKnownTypes;
using Signature = Catalyst.Core.Modules.Cryptography.BulletProofs.Types.Signature;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs
{
    public static class NativeBinding
    {
        private const string Library = "catalystffi";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int generate_key(byte[] bytes);

        internal static IPrivateKey GeneratePrivateKey()
        {
            var key = new byte[PrivateKeyLength];
            var error_code = generate_key(key);
            if (ErrorCode.TryParse<ErrorCode>(error_code.ToString(), out var errorCode) && errorCode != ErrorCode.NoError)
            {
                Error.ThrowErrorFromErrorCode(errorCode);
            }

            return new PrivateKey(key);
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int std_sign(byte[] signature, byte[] publicKey, byte[] privateKey, byte[] message, int messageLength, byte[] context, int contextLength);

        internal static ISignature StdSign(byte[] privateKey, byte[] message, byte[] context)
        {
            if (privateKey.Length != PrivateKeyLength)
            {
                Error.ThrowArgumentExceptionPrivateKeyLength(PrivateKeyLength);
            }

            var signature = new byte[SignatureLength];
            var publicKey = new byte[PublicKeyLength];

            var error_code = std_sign(signature, publicKey, privateKey, message, message.Length, context, context.Length);
            
            if (ErrorCode.TryParse<ErrorCode>(error_code.ToString(), out var errorCode) && errorCode != ErrorCode.NoError)
            {
                Error.ThrowErrorFromErrorCode(errorCode);
            }

            return new Signature(signature, publicKey);
        }
        
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int std_verify(byte[] signature, byte[] publicKey, byte[] message, int messageLength, byte[] context, int contextLength);

        internal static bool StdVerify(byte[] signature, byte[] publicKey, byte[] message, byte[] context)
        {
            if (signature.Length != SignatureLength)
            {
                Error.ThrowArgumentExceptionSignatureLength(SignatureLength);
            }

            if (publicKey.Length != PublicKeyLength)
            {
                Error.ThrowArgumentExceptionPublicKeyLength(PublicKeyLength);
            }

            var error_code = std_verify(signature, publicKey, message, message.Length, context, context.Length);

            ErrorCode.TryParse<ErrorCode>(error_code.ToString(), out var errorCode);

            if (errorCode == ErrorCode.NoError)
            {
                return true;
            }

            if (errorCode != ErrorCode.SignatureVerificationFailure)
            {
                Error.ThrowErrorFromErrorCode(errorCode);
            }

            return false;
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int validate_public_key(byte[] publicKey);

        internal static void ValidatePublicKeyOrThrow(byte[] publicKey)
        {
            if (publicKey.Length != PublicKeyLength)
            { 
                Error.ThrowArgumentExceptionPublicKeyLength(PublicKeyLength);
            } 

            var error_code = validate_public_key(publicKey);

            if (ErrorCode.TryParse<ErrorCode>(error_code.ToString(), out var errorCode) && errorCode != ErrorCode.NoError)
            {
                Error.ThrowErrorFromErrorCode(errorCode);
            }
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void publickey_from_private(byte[] publicKey, byte[] privateKey);

        internal static IPublicKey GetPublicKeyFromPrivate(byte[] privateKey)
        {
            if (privateKey.Length != PrivateKeyLength)
            {
                Error.ThrowArgumentExceptionPrivateKeyLength(PrivateKeyLength);
            }

            var publicKeyBytes = new byte[PublicKeyLength];
            publickey_from_private(publicKeyBytes, privateKey);
            
            return new PublicKey(publicKeyBytes);
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_private_key_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_public_key_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_signature_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_max_context_length();

        private static int _privateKeyLength;

        public static int PrivateKeyLength
        {
            get
            {
                if (_privateKeyLength == 0)
                {
                    _privateKeyLength = get_private_key_length(); 
                }

                return _privateKeyLength;
            }
        }

        private static int _publicKeyLength;

        public static int PublicKeyLength
        {
            get
            {
                if (_publicKeyLength == 0)
                {
                    _publicKeyLength = get_public_key_length(); 
                }

                return _publicKeyLength;
            }
        }

        private static int _signatureLength;

        public static int SignatureLength
        {
            get
            {
                if (_signatureLength == 0)
                {
                    _signatureLength = get_signature_length(); 
                }

                return _signatureLength;
            }
        }

        private static int _signatureContextMaxLength;

        public static int SignatureContextMaxLength
        {
            get
            {
                if (_signatureContextMaxLength == 0)
                {
                    _signatureContextMaxLength = get_max_context_length(); 
                }

                return _signatureContextMaxLength;
            }
        }
    }
}
