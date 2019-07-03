using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Network;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using Nethereum.RLP;
using System.Net;

namespace Catalyst.Cli.Commands
{
    public class PeerRemoveCommand : CommandBase<RemovePeerRequest, IRemovePeerOptions>
    {
        public PeerRemoveCommand(IOptionsBase optionBase, ICommandContext commandContext) : base(optionBase, commandContext) { }

        public override RemovePeerRequest GetMessage(IRemovePeerOptions option)
        {
            return new RemovePeerRequest
            {
                PeerIp = ByteString.CopyFrom(IPAddress.Parse(option.Ip).To16Bytes()),
                PublicKey = string.IsNullOrEmpty(option.PublicKey)
                    ? ByteString.Empty
                    : ByteString.CopyFrom(option.PublicKey.ToBytesForRLPEncoding())
            };
        }
    }
}
