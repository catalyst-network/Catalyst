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

using Catalyst.Core.Lib.Extensions;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.Extensions
{
    public static class CidExtensions
    {
        // @TODO get this from hashing algorithm. as a param to the extension method
        private const string Encoding = "base32";
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="multiHash"></param>
        /// <returns></returns>
        public static Cid ToCid(this MultiHash multiHash)
        {
            return new Cid
            {
                Version = 1,
                Hash = multiHash,
                ContentType = "dag-pb",
                Encoding = Encoding
            };
        }

        public static Cid ToCid(this byte[] cid)
        {
            return Cid.Decode(MultiBase.Encode(cid, Encoding));
        }

        public static Cid ToCid(this string cid)
        {
            //MultiBase.Encode(cid.ToUtf8Bytes(), Encoding)
            return Cid.Decode(cid);
        }
    }
}
