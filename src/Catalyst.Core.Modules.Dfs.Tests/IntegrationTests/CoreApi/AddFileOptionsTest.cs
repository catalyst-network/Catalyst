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

using System;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class AddFileOptionsTests
    {
        [Test]
        public void Defaults()
        {
            var options = new AddFileOptions();

            Assert.Equals(true, options.Pin);
            Assert.Equals(256 * 1024, options.ChunkSize);
            Assert.Equals(MultiHash.DefaultAlgorithmName, options.Hash);
            Assert.Equals(false, options.OnlyHash);
            Assert.Equals(false, options.RawLeaves);
            Assert.Equals(false, options.Trickle);
            Assert.Equals(false, options.Wrap);
            Assert.That(options.Progress, Is.Null);
            Assert.That(options.ProtectionKey, Is.Null);
        }

        [Test]
        public void Setting()
        {
            var options = new AddFileOptions
            {
                Pin = false,
                ChunkSize = 2 * 1024,
                Hash = "sha2-512",
                OnlyHash = true,
                RawLeaves = true,
                Progress = new Progress<TransferProgress>(),
                Trickle = true,
                Wrap = true,
                ProtectionKey = "secret"
            };

            Assert.Equals(false, options.Pin);
            Assert.Equals(2 * 1024, options.ChunkSize);
            Assert.Equals("sha2-512", options.Hash);
            Assert.Equals(true, options.OnlyHash);
            Assert.Equals(true, options.RawLeaves);
            Assert.Equals(true, options.Trickle);
            Assert.Equals(true, options.Wrap);
            Assert.That(options.Progress, Is.Not.Null);
            Assert.Equals("secret", options.ProtectionKey);
        }
    }
}
