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

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    // public sealed class SignMessageResponseHandlerTests : IDisposable
    // {
    //     private readonly ILogger _logger;
    //     private readonly IChannelHandlerContext _fakeContext;
    //     public static readonly List<object[]> QueryContents;
    //     
    //     private readonly IUserOutput _output;
    //     private SignMessageResponseHandler _handler;
    //
    //     //@TODO why not mock the actual response object? if we ever change it then the test will pass but fail in real world
    //     public struct SignedResponse
    //     {
    //         internal ByteString Signature;
    //         internal ByteString PublicKey;
    //         internal ByteString OriginalMessage;
    //     }
    //
    //     static SignMessageResponseHandlerTests()
    //     {
    //         QueryContents = new List<object[]>
    //         {
    //             new object[]
    //             {
    //                 SignMessage($@"hello", $@"this is a fake signature", $@"this is a fake public key")
    //             },
    //             new object[]
    //             {
    //                 SignMessage("", "", "")
    //             },
    //         };
    //     }
    //     
    //     public SignMessageResponseHandlerTest()
    //     {
    //         _logger = Substitute.For<ILogger>();
    //         _fakeContext = Substitute.For<IChannelHandlerContext>();
    //         _output = Substitute.For<IUserOutput>();
    //     }
    //
    //     private static SignedResponse SignMessage(string messageToSign, string signature, string pubKey)
    //     {
    //         var signedResponse = new SignedResponse
    //         {
    //             Signature = signature.ToUtf8ByteString(),
    //             PublicKey = pubKey.ToUtf8ByteString(),
    //             OriginalMessage = RLP.EncodeElement(messageToSign.ToBytesForRLPEncoding()).ToByteString()
    //         };
    //
    //         return signedResponse;
    //     }
    //
    //     [Theory]
    //     [MemberData(nameof(QueryContents))]  
    //     public async Task RpcClient_Can_Handle_SignMessageResponse(SignedResponse signedResponse)
    //     {
    //         var response = new MessageFactory().GetMessage(new MessageDto(
    //                 new SignMessageResponse
    //                 {
    //                     OriginalMessage = signedResponse.OriginalMessage,
    //                     PublicKey = signedResponse.PublicKey,
    //                     Signature = signedResponse.Signature
    //                 },
    //                 MessageTypes.Tell,
    //                 PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
    //                 PeerIdentifierHelper.GetPeerIdentifier("sender_key")),                
    //             CorrelationId.GenerateCorrelationId());
    //
    //         var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response);
    //         
    //         _handler = new SignMessageResponseHandler(_output, _logger);
    //         _handler.StartObserving(messageStream);
    //
    //         await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();
    //
    //         _output.Received(1).WriteLine(Arg.Any<string>());
    //     }
    //     
    //     public void Dispose()
    //     {
    //         _handler?.Dispose();
    //     }
    // }
}
