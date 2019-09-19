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

namespace Catalyst.Core.Modules.Kvm
{
    // | International System | Si Name    | Si Symbol | Usual name |
    // |----------------------|------------|-----------|------------|
    // | 10-^18               | attokatal  | akat      | mol        |
    // | 10-^15               | femtokatal | fkat      | kmol       |
    // | 10-^12               | picokatal  | pkat      | mmol       |
    // | 10-^9                | nanokatal  | nkat      | gmol       |
    // | 10-^6                | microkatal | µkat      | bernstein  |
    // | 10-^3                | millikatal | mkat      | kitte      |
    // | 1                    | Katal      | KAT       | Katal      |
    // | 10^3                 | kilokatal  | kkat      | KitKat     |
    // | 10^6                 | megakatal  | Mkat      | mkata      |
    // | 10^9                 | gigakatal  | Gkat      | gkata      |
    // | 10^12                | terakatal  | Tkat      | tkata      |
    public static class CatalystUnit
    {
        public static readonly UInt256 Mol = 1;
        public static readonly UInt256 Gmol = 1_000_000_000;
        public static readonly UInt256 Katal = 1_000_000_000_000;
        public static readonly string KatalSymbol = "KAT";
    }
}
