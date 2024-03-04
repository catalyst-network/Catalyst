// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Threading;
using Nethermind.Core;

namespace Catalyst.Core.Modules.Kvm
{
    public class KeyValueStore : IKeyValueStore
    {
        public KeyValueStore()
        {
        }

        private static KeyValueStore _instance;
        public static KeyValueStore Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new KeyValueStore());

        public byte[]? Get(ReadOnlySpan<byte> key, ReadFlags flags = ReadFlags.None)
        {
            return null;
        }

        public void Set(ReadOnlySpan<byte> key, byte[]? value, WriteFlags flags = WriteFlags.None)
        {
            throw new NotSupportedException();
        }
    }
}
