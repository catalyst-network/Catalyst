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
using System.Linq;

namespace Lib.P2P
{
    /// <summary>
    ///   A sequence of targets that are approved.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    /// <remarks>
    ///   Only targets that are defined will pass.  If no targets are defined, then anything
    ///   passes.
    /// </remarks>
    public class WhiteList<T> : ConcurrentBag<T>, IPolicy<T>
        where T : IEquatable<T>
    {
        /// <inheritdoc />
        public bool IsAllowed(T target) { return IsEmpty || this.Contains(target); }
    }
}
