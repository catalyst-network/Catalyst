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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.TestUtils;
using NUnit.Framework;


namespace Catalyst.Core.Lib.Tests.IntegrationTests.FileSystem
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class FileStoreTest : FileSystemBasedTest
    {
        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);
        }

        private sealed class Entity
        {
            public int Number;
            public string? Value;
        }

        private readonly Entity _a = new Entity {Number = 1, Value = "a"};
        private readonly Entity _b = new Entity {Number = 2, Value = "b"};

        private FileStore<int, Entity> Store
        {
            get
            {
                var folder = Path.Combine(FileSystem.GetCatalystDataDir().FullName, "test-filestore");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                return new FileStore<int, Entity>
                {
                    Folder = folder,
                    NameToKey = name => name.ToString(),
                    KeyToName = int.Parse
                };
            }
        }

        [Test]
        public async Task PutAndGet()
        {
            var store = Store;

            await store.PutAsync(_a.Number, _a);
            await store.PutAsync(_b.Number, _b);

            var a1 = await store.GetAsync(_a.Number);
            Assert.Equals(_a.Number, a1.Number);
            Assert.Equals(_a.Value, a1.Value);

            var b1 = await store.GetAsync(_b.Number);
            Assert.Equals(_b.Number, b1.Number);
            Assert.Equals(_b.Value, b1.Value);
        }

        [Test]
        public async Task TryGet()
        {
            var store = Store;
            await store.PutAsync(3, _a);
            var a1 = await store.GetAsync(3);
            Assert.Equals(_a.Number, a1.Number);
            Assert.Equals(_a.Value, a1.Value);

            var a3 = await store.TryGetAsync(42);
            Assert.That(a3, Is.Null);
        }

        [Test]
        public void Get_Unknown()
        {
            ExceptionAssert.Throws<KeyNotFoundException>(() =>
            {
                var _ = Store.GetAsync(42).Result;
            });
        }

        [Test]
        public async Task Remove()
        {
            var store = Store;
            await store.PutAsync(4, _a);
            Assert.That(await store.TryGetAsync(4), Is.Not.Null);

            await store.RemoveAsync(4);
            Assert.That(await store.TryGetAsync(4), Is.Null);
        }

        [Test]
        public async Task Remove_Unknown()
        {
            var store = Store;
            await store.RemoveAsync(5);
        }

        [Test]
        public async Task Length()
        {
            var store = Store;
            await store.PutAsync(6, _a);
            var length = await store.LengthAsync(6);
            Assert.Equals(length.HasValue, true);
            Assert.That(length.Value, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task Length_Unknown()
        {
            var store = Store;
            var length = await store.LengthAsync(7);
            Assert.Equals(length.HasValue, false);
        }

        [Test]
        public async Task Values()
        {
            var store = Store;
            await store.PutAsync(8, new Entity {Value = "v0"});
            await store.PutAsync(9, new Entity {Value = "v1"});
            await store.PutAsync(10, new Entity {Value = "v0"});
            var values = Store.Values.Where(e => e.Value == "v0").ToArray();
            Assert.Equals(2, values.Length);
        }

        [Test]
        public async Task Names()
        {
            var store = Store;
            await store.PutAsync(11, _a);
            await store.PutAsync(12, _a);
            await store.PutAsync(13, _a);
            var names = Store.Names.Where(n => n == 11 || n == 13).ToArray();
            Assert.Equals(2, names.Length);
        }

        [Test]
        public async Task Atomic()
        {
            var store = Store;
            const int nTasks = 100;
            var tasks = Enumerable
               .Range(1, nTasks)
               .Select(i => Task.Run(() => AtomicTask(store)))
               .ToArray();
            await Task.WhenAll(tasks);
        }

        private async Task AtomicTask(FileStore<int, Entity> store)
        {
            await store.PutAsync(1, _a);
            await store.TryGetAsync(1);
            await store.RemoveAsync(1);
        }

        [Test]
        public void PutWithException()
        {
            Task BadSerialize(Stream stream, int name, Entity value, CancellationToken cancel) => throw new Exception("no serializer");
            var store = Store;
            store.Serialize = BadSerialize;

            ExceptionAssert.Throws<Exception>(() => store.PutAsync(_a.Number, _a).Wait());
            Assert.Equals(store.ExistsAsync(_a.Number).Result, false);
        }
    }
}
