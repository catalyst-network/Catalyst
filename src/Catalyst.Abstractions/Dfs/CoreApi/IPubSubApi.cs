#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Lib.P2P.PubSub;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Allows you to publish messages to a given topic, and also to
    ///   subscribe to new messages on a given topic.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This is an experimental feature. It is not intended in its current state
    ///   to be used in a production environment.
    ///   </para>
    ///   <para>
    ///   To use, the daemon must be run with '--enable-pubsub-experiment'.
    ///   </para>
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/PUBSUB.md">Pubsub API spec</seealso>
    public interface IPubSubApi : IPubSub { }
}
