using System;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Google.Protobuf;

namespace Catalyst.Cli.Commands
{
    public abstract class CommandBase<T, TOption> : ICommandBase<T, TOption>
        where T : IMessage<T>
        where TOption : IOptionsBase
    {
        private readonly IOptionsBase _optionsBase;

        protected CommandBase(IOptionsBase optionBase, ICommandContext commandContext)
        {
            _optionsBase = optionBase;
            CommandContext = commandContext;
        }

        public virtual bool SendMessage(TOption options)
        {
            var message = GetMessage(options);

            if (message != null)
            {
                var messageDto = CommandContext.DtoFactory.GetDto(
                    message.ToProtocolMessage(SenderPeerIdentifier.PeerId),
                    SenderPeerIdentifier,
                    RecipientPeerIdentifier);
                Target.SendMessage(messageDto);
            }

            return true;
        }

        public abstract T GetMessage(TOption option);

        protected ICommandContext CommandContext { get; }

        protected IPeerIdentifier RecipientPeerIdentifier => PeerIdentifier.BuildPeerIdFromConfig(CommandContext.GetNodeConfig(_optionsBase.Node), CommandContext.PeerIdClientId);

        protected IPeerIdentifier SenderPeerIdentifier => CommandContext.PeerIdentifier;

        public INodeRpcClient Target => CommandContext.GetConnectedNode(_optionsBase.Node);

        public Type GetOptionType() { return typeof(TOption); }
    }
}
