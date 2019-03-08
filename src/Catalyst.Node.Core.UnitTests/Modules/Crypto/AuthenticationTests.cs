using FluentAssertions;
using NSubstitute;
using Xunit;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Modules.Crypto;
using Catalyst.Node.Core.Modules.Authentication;

namespace Catalyst.Node.Core.UnitTest.Modules.Crypto
{
    public class AuthenticationTests
    {
        private readonly Crypto _crypto;
        private readonly ICryptoContext _context;
        private readonly ISignatureProvider _signatureProvider;

        public AuthenticationTests()
        {
            _context = Substitute.For<ICryptoContext>();
            _signatureProvider = Substitute.For<ISignatureProvider>();
            _crypto = new Crypto(_context, _signatureProvider);
        }
        
        //TODO tests after fix for System.InvalidProgramException : Cannot create boxed ByRef-like values. Issue with NSubstitue and Span<> types.
        
    }
}