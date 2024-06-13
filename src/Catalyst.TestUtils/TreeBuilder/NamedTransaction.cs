// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using System.Diagnostics;

namespace Catalyst.TestUtils.Repository.TreeBuilder
{
    [DebuggerDisplay(nameof(Name))]
    public class NamedTransaction : Transaction
    {
        public string Name { get; set; } = null!;

        public override string ToString() => Name;
    }
}
