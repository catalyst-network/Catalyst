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
    ///   A rule that must be enforced.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public interface IPolicy<T>
    {
        /// <summary>
        ///   Determines if the target passes the rule.
        /// </summary>
        /// <param name="target">
        ///   An object to test against the rule.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the <paramref name="target"/> passes the rule;
        ///   otherwise <b>false</b>.
        /// </returns>
        bool IsAllowed(T target);
    }
}
