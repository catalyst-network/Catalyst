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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests
{
    [TestClass]
    public class StreamExtensionsTest
    {
        [TestMethod]
        public async Task ReadAsync()
        {
            var expected = new byte[] {1, 2, 3, 4};
            using (var ms = new MemoryStream(expected))
            {
                var actual = new byte[expected.Length];
                await ms.ReadExactAsync(actual, 0, actual.Length);
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void ReadAsync_EOS()
        {
            var expected = new byte[] {1, 2, 3, 4};
            var actual = new byte[expected.Length + 1];

            using (var ms = new MemoryStream(expected))
            {
                ExceptionAssert.Throws<EndOfStreamException>(() =>
                {
                    ms.ReadExactAsync(actual, 0, actual.Length).Wait();
                });
            }

            var cancel = new CancellationTokenSource();
            using (var ms = new MemoryStream(expected))
            {
                ExceptionAssert.Throws<EndOfStreamException>(() =>
                {
                    ms.ReadExactAsync(actual, 0, actual.Length, cancel.Token).Wait();
                });
            }
        }

        [TestMethod]
        public async Task ReadAsync_Cancel()
        {
            var expected = new byte[] {1, 2, 3, 4};
            var actual = new byte[expected.Length];
            var cancel = new CancellationTokenSource();
            using (var ms = new MemoryStream(expected))
            {
                await ms.ReadExactAsync(actual, 0, actual.Length, cancel.Token);
                CollectionAssert.AreEqual(expected, actual);
            }

            cancel.Cancel();
            using (var ms = new MemoryStream(expected))
            {
                ExceptionAssert.Throws<TaskCanceledException>(() =>
                {
                    ms.ReadExactAsync(actual, 0, actual.Length, cancel.Token).Wait();
                });
            }
        }
    }
}
