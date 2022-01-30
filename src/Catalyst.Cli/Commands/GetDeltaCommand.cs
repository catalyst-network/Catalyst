#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Cli.CommandTypes;
using Catalyst.Cli.Options;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Modules.Network.Dotnetty.Abstractions.Cli.Commands;
using Catalyst.Protocol.Rpc.Node;
using MultiFormats;
using Serilog;

namespace Catalyst.Cli.Commands
{
    public sealed class GetDeltaCommand : BaseMessageCommand<GetDeltaRequest, GetDeltaResponse, GetDeltaOptions>
    {
        public static string UnableToRetrieveDeltaMessage => "Unable to retrieve delta.";

        public GetDeltaCommand(ICommandContext commandContext, ILogger logger) : base(commandContext, logger) { }

        protected override GetDeltaRequest GetMessage(GetDeltaOptions option)
        {
            try
            {
                var hashBytes = MultiBase.Decode(option.Hash);
                var cid = hashBytes.ToCid();
                new MultiHash(cid.Hash.ToArray());
                return new GetDeltaRequest {DeltaDfsHash = hashBytes.ToByteString()};
            }
            catch (FormatException fe)
            {
                Log.Warning("Unable to parse hash {0} as a Cid {1}", option.Hash, fe);
                CommandContext.UserOutput.WriteLine($"Unable to parse hash {option.Hash} as a Cid");
                return default;
            }
        }

        protected override void ResponseMessage(GetDeltaResponse response)
        {
            if (response.Delta == null)
            {
                CommandContext.UserOutput.WriteLine(UnableToRetrieveDeltaMessage);
                return;
            }

            CommandContext.UserOutput.WriteLine(response.Delta.ToJsonString());
        }
    }
}
