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

using System;
using System.Collections.Concurrent;
using Catalyst.Abstractions.P2P.Discovery;

namespace Catalyst.Core.P2P.Discovery.Hastings
{
    public sealed class HastingsCareTaker : IHastingsCareTaker
    {
        public ConcurrentStack<IHastingsMemento> HastingMementoList { get; }

        public HastingsCareTaker()
        {
            HastingMementoList = new ConcurrentStack<IHastingsMemento>();
        }

        /// <inheritdoc />
        public void Add(IHastingsMemento hastingsMemento)
        {
            HastingMementoList.Push(hastingsMemento);
        }

        public IHastingsMemento Peek()
        {
            if (!HastingMementoList.TryPeek(out IHastingsMemento latest))
            {
                throw new InvalidOperationException("No memento found in queue");
            }

            return latest;
        }

        /// <inheritdoc />
        public IHastingsMemento Get()
        {
            if (HastingMementoList.Count >= 2)
            {
                if (HastingMementoList.TryPop(out var hastingMemento))
                {
                    return hastingMemento;
                }
            }
            else if (HastingMementoList.Count == 1)
            {
                HastingMementoList.TryPeek(out var hastingMemento);
                return hastingMemento;
            }

            throw new InvalidOperationException("No memento found in queue");
        }
    }
}
