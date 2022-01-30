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

namespace Lib.P2P
{
    /// <summary>
    ///   A base for defining a policy.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public abstract class Policy<T> : IPolicy<T>
    {
        /// <inheritdoc />
        public abstract bool IsAllowed(T target);

        /// <inheritdoc />
        public bool IsNotAllowed(T target) { return !IsAllowed(target); }
    }

    /// <summary>
    ///   A rule that always passes.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public class PolicyAlways<T> : Policy<T>
    {
        /// <inheritdoc />
        public override bool IsAllowed(T target) { return true; }
    }

    /// <summary>
    ///   A rule that always fails.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public class PolicyNever<T> : Policy<T>
    {
        /// <inheritdoc />
        public override bool IsAllowed(T target) { return false; }
    }
}
