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

namespace Catalyst.Abstractions.P2P.Discovery
{
    /// <summary>
    ///     Caretaker of memento pattern is responsible for the memento's safekeeping,
    ///     never operates on or examines the contents of a memento.
    ///     https://www.dofactory.com/net/memento-design-pattern
    /// </summary>
    public interface IHastingsCareTaker
    {
        ConcurrentStack<IHastingsMemento> HastingMementoList { get; }

        /// <summary>
        ///     Adds a new state from the walk on top of the <see cref="HastingMementoList"/>.
        /// </summary>
        /// <param name="hastingsMemento"></param>
        void Add(IHastingsMemento hastingsMemento);

        /// <summary>
        ///     Gets the last state of the walk from the queue and pops it off the <see cref="HastingMementoList"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">When the <see cref="HastingMementoList"/> is empty.</exception>
        IHastingsMemento Get();

        /// <summary>
        ///     Returns the item at the top of the top of the <see cref="HastingMementoList"/> without popping it.
        /// </summary>
        /// <returns>The memento currently at the top of the queue if any.</returns>
        /// <exception cref="InvalidOperationException">When the <see cref="HastingMementoList"/> is empty.</exception>
        IHastingsMemento Peek();
    }
}
