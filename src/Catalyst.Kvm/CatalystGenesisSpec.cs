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

using System.Threading;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Core.Specs.Forks;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Kvm
{
    public sealed class CatalystGenesisSpec : IReleaseSpec
    {
        private static CatalystGenesisSpec _instance;

        public static CatalystGenesisSpec Instance
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instance, () => new CatalystGenesisSpec());
                return _instance;
            }
        }

        private CatalystGenesisSpec() { }
        
        public long MaximumExtraDataSize { get; } = Istanbul.Instance.MaximumExtraDataSize;
        public long MaxCodeSize { get; } = Istanbul.Instance.MaxCodeSize;
        public long MinGasLimit { get; } = Istanbul.Instance.MinGasLimit;
        public long GasLimitBoundDivisor { get; } = Istanbul.Instance.GasLimitBoundDivisor;
        public Address Registrar { get; } = Istanbul.Instance.Registrar;
        public UInt256 BlockReward { get; } = Istanbul.Instance.BlockReward;
        public long DifficultyBombDelay { get; } = Istanbul.Instance.DifficultyBombDelay;
        public long DifficultyBoundDivisor { get; } = Istanbul.Instance.DifficultyBoundDivisor;
        public int MaximumUncleCount { get; } = Istanbul.Instance.MaximumUncleCount;
        public bool IsTimeAdjustmentPostOlympic { get; } = Istanbul.Instance.IsTimeAdjustmentPostOlympic;
        public bool IsEip2Enabled { get; } = Istanbul.Instance.IsEip2Enabled;
        public bool IsEip7Enabled { get; } = Istanbul.Instance.IsEip7Enabled;
        public bool IsEip100Enabled { get; } = Istanbul.Instance.IsEip100Enabled;
        public bool IsEip140Enabled { get; } = Istanbul.Instance.IsEip140Enabled;
        public bool IsEip150Enabled { get; } = Istanbul.Instance.IsEip150Enabled;
        public bool IsEip155Enabled { get; } = Istanbul.Instance.IsEip155Enabled;
        public bool IsEip158Enabled { get; } = Istanbul.Instance.IsEip158Enabled;
        public bool IsEip160Enabled { get; } = Istanbul.Instance.IsEip160Enabled;
        public bool IsEip170Enabled { get; } = Istanbul.Instance.IsEip170Enabled;
        public bool IsEip196Enabled { get; } = Istanbul.Instance.IsEip196Enabled;
        public bool IsEip197Enabled { get; } = Istanbul.Instance.IsEip197Enabled;
        public bool IsEip198Enabled { get; } = Istanbul.Instance.IsEip198Enabled;
        public bool IsEip211Enabled { get; } = Istanbul.Instance.IsEip211Enabled;
        public bool IsEip214Enabled { get; } = Istanbul.Instance.IsEip214Enabled;
        public bool IsEip649Enabled { get; } = Istanbul.Instance.IsEip649Enabled;
        public bool IsEip658Enabled { get; } = Istanbul.Instance.IsEip658Enabled;
        public bool IsEip145Enabled { get; } = Istanbul.Instance.IsEip145Enabled;
        public bool IsEip1014Enabled { get; } = Istanbul.Instance.IsEip1014Enabled;
        public bool IsEip1052Enabled { get; } = Istanbul.Instance.IsEip1052Enabled;
        public bool IsEip1283Enabled { get; } = Istanbul.Instance.IsEip1283Enabled;
        public bool IsEip1234Enabled { get; } = Istanbul.Instance.IsEip1234Enabled;
        public bool IsEip1344Enabled { get; } = Istanbul.Instance.IsEip1344Enabled;
        public bool IsEip2028Enabled { get; } = Istanbul.Instance.IsEip2028Enabled;
        public bool IsEip152Enabled { get; } = Istanbul.Instance.IsEip152Enabled;
        public bool IsEip1108Enabled { get; } = Istanbul.Instance.IsEip1108Enabled;
        public bool IsEip1884Enabled { get; } = Istanbul.Instance.IsEip1884Enabled;
        public bool IsEip2200Enabled { get; } = Istanbul.Instance.IsEip2200Enabled;
        public bool IsEip158IgnoredAccount(Address address) { return address == Address.SystemUser; }
    }
}