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

using AutoMapper;
using Catalyst.Core.Lib.DAO.Converters;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Lib.DAO
{
    public class AccountDao : DaoBase<Account, AccountDao>
    {
        public string Balance { get; set; }
        public string Nonce { get; set; }
        public string CodeHash { get; set; }
        public string StorageRoot { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Account, AccountDao>()
               .ForMember(e => e.Balance,
                    opt => opt.ConvertUsing<UInt256ToStringConverter, UInt256>())
               .ForMember(e => e.CodeHash,
                    opt => opt.ConvertUsing<KeccakToStringConverter, Keccak>())
               .ForMember(e => e.StorageRoot,
                    opt => opt.ConvertUsing<KeccakToStringConverter, Keccak>())
               .ForMember(e => e.Nonce,
                    opt => opt.ConvertUsing<UInt256ToStringConverter, UInt256>());

            cfg.CreateMap<AccountDao, Account>()
               .ForMember(e => e.Balance,
                    opt => opt.ConvertUsing<StringToUInt256Converter, string>())
               .ForMember(e => e.StorageRoot,
                    opt => opt.ConvertUsing<StringToKeccakConverter, string>())
               .ForMember(e => e.CodeHash,
                    opt => opt.ConvertUsing<StringToKeccakConverter, string>())
               .ForMember(e => e.Nonce,
                    opt => opt.ConvertUsing<StringToUInt256Converter, string>());
        }
    }
}
