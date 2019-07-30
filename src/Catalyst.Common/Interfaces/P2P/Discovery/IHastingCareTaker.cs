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

namespace Catalyst.Common.Interfaces.P2P.Discovery
{
    /// <summary>
    ///     Caretaker of memento pattern is responsible for the memento's safekeeping,
    ///     never operates on or examines the contents of a memento.
    ///     https://www.dofactory.com/net/memento-design-pattern
    /// </summary>
    public interface IHastingCareTaker
    {
        ConcurrentStack<IHastingMemento> HastingMementoList { get; }

        /// <summary>
        ///     Adds a new state from the walk to the queue
        /// </summary>
        /// <param name="hastingMemento"></param>
        void Add(IHastingMemento hastingMemento);

        /// <summary>
        ///     Gets the last state of the walk from the queue
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        IHastingMemento Get();
        IHastingMemento Peek();

    }
}
