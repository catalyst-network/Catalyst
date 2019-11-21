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

using MultiFormats;
using PeerTalk;

namespace Catalyst.Core.Lib.Util
{
    public static class CidHelper
    {
        public static readonly string Encoding = "base32";

        /// <summary>
        ///     @TODO WE PASS IN MULTIHASH THEN COMPUTE THE HASH OF THE HASH!!!!!
        /// </summary>
        /// <param name="multiHash"></param>
        /// <returns></returns>
        public static Cid CreateCid(MultiHash multiHash)
        {
            return new Cid {Version = 1, Hash = MultiHash.ComputeHash(multiHash.Digest), ContentType = "raw", Encoding = Encoding};
        }

        public static Cid Cast(byte[] cid) { return Cid.Decode(MultiBase.Encode(cid, Encoding)); }
    }
}
