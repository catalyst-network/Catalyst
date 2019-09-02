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

using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Kvm
{
    public static class KvmIntExtensions
    {
        public static ulong GB(this int @this) { return (ulong) @this * 1024UL * 1024UL * 1024UL; }

        public static ulong MB(this int @this) { return (ulong) @this * 1024UL * 1024UL; }

        public static ulong KB(this int @this) { return (ulong) @this * 1024UL; }

        public static UInt256 Kat(this int @this) { return (uint) @this * CatalystUnit.Kat; }

        public static UInt256 Ful(this int @this) { return (uint) @this * CatalystUnit.Fulhame; }

        public static UInt256 GFul(this int @this) { return (uint) @this * CatalystUnit.GFulhame; }
    }
}
