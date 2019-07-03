using System;
using System.Collections.Generic;
using System.Text;
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
        private readonly ICommandContext _commandContext;

        public CommandBase(IOptionsBase optionBase, ICommandContext commandContext)
        {
            _optionsBase = optionBase;
            _commandContext = commandContext;
        }

        public virtual bool SendMessage(TOption options)
        {
            T message = GetMessage(options);

            if (message != null)
            {
                var messageDto = _commandContext.DtoFactory.GetDto(
                    message.ToProtocolMessage(SenderPeerIdentifier.PeerId),
                    SenderPeerIdentifier,
                    RecipientPeerIdentifier);
                Target.SendMessage(messageDto);
            }

            return true;
        }

        public abstract T GetMessage(TOption option);

        protected ICommandContext CommandContext => _commandContext;

        protected IPeerIdentifier RecipientPeerIdentifier => PeerIdentifier.BuildPeerIdFromConfig(_commandContext.GetNodeConfig(_optionsBase.Node), _commandContext.PeerIdClientId);

        protected IPeerIdentifier SenderPeerIdentifier => _commandContext.PeerIdentifier;

        public INodeRpcClient Target => _commandContext.GetConnectedNode(_optionsBase.Node);

        public Type GetOptionType() { return typeof(TOption); }
    }
}
