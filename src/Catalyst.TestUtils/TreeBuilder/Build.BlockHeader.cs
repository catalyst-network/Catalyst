// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Catalyst.TestUtils.Repository.TreeBuilder
{
    public partial class Build
    {
        public BlockHeaderBuilder BlockHeader => new();
        public BlockHeader EmptyBlockHeader => BlockHeader.TestObject;
    }
}
