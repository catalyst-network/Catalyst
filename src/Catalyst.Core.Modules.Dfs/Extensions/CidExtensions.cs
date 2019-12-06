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

using LibP2P;
using TheDotNetLeague.MultiFormats.MultiBase;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Core.Modules.Dfs.Extensions
{
    public static class CidExtensions
    {
        private static readonly string Encoding = "base32";

        public static Cid CreateCid(this MultiHash multiHash)
        {
            return new Cid {Version = 1, Hash = multiHash, ContentType = "raw", Encoding = Encoding};
        }

        public static Cid ToCid(this byte[] cid) { return Cid.Decode(MultiBase.Encode(cid, Encoding)); }
    }
}
