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

using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Cli.CommandTypes;
using Catalyst.Cli.Options;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Rpc.Node;
using Serilog;

namespace Catalyst.Cli.Commands
{
    public sealed class
        MessageVerifyCommand : BaseMessageCommand<VerifyMessageRequest, VerifyMessageResponse, VerifyOptions>
    {
        public MessageVerifyCommand(ICommandContext commandContext, ILogger logger)
            : base(commandContext, logger) { }

        protected override VerifyMessageRequest GetMessage(VerifyOptions option)
        {
            return new VerifyMessageRequest
            {
                Message = option.Message.Trim('\"').ToUtf8ByteString(),
                PublicKey = option.Address.ToUtf8ByteString(),
                Signature = option.Signature.ToUtf8ByteString()
            };
        }
    }
}
