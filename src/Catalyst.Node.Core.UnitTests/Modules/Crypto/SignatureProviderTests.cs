using System;
using System.Text;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Helpers.Keystore;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.Modules.Authentication;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Authentication
{
    public class SignatureProviderTests
    {
        private readonly ICryptoContext _context;
        private readonly IKeystore _keystore;
        private readonly ISignatureProvider _signatureProvider;

        public SignatureProviderTests()
        {
            _context = Substitute.For<ICryptoContext>();
            _keystore = Substitute.For<IKeystore>();
            _signatureProvider = new LocalSignatureProvider(_context, _keystore);
        }
        //TODO Fix System.InvalidProgramException : Cannot create boxed ByRef-like values. Issue with NSubstitue and Span<> types.
        /*[Fact]
        public void SignShouldRetrieveCorrectKeyFromStoreAndUseItToSign()
        {
            var message = Encoding.UTF8.GetBytes("anything for now");
            ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(message);
            string address = "12345";
            string password = "password123";
            IPrivateKey key = _context.GeneratePrivateKey();

            _keystore.RetrieveKey(address, password).Returns(key);
            
            _signatureProvider.Sign(bytes, address, password);

            _keystore.Received(1).RetrieveKey(address, password);
            _context.Received(1).Sign(key, bytes);

        }*/
    }
}