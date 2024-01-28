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
using System.Threading.Tasks;

namespace Catalyst.KBucket
{
    public sealed class CollectionTest
    {
        [Test]
        public void Add()
        {
            var bucket = new KBucket<Contact>();
            var x = new Contact("1");
            bucket.Add(x);
            Assert.Equals(1, bucket.Count);
            Assert.That(bucket.Contains(x), Is.True);
        }

        [Test]
        public void AddDuplicate()
        {
            var bucket = new KBucket<Contact>();
            var x = new Contact("1");
            bucket.Add(x);
            bucket.Add(x);
            Assert.Equals(1, bucket.Count);
            Assert.That(bucket.Contains(x), Is.True);
        }

        [Test]
        public void AddBadContact()
        {
            var bucket = new KBucket<Contact>();
            ExceptionAssert.Throws<ArgumentNullException>(() => bucket.Add(null));
            ExceptionAssert.Throws<ArgumentNullException>(() => bucket.Add(new Contact("a") {Id = null}));
            ExceptionAssert.Throws<ArgumentNullException>(() => bucket.Add(new Contact("a") {Id = new byte[0]}));
        }

        [Test]
        public void TryGet()
        {
            var bucket = new KBucket<Contact>();
            var alpha = new Contact("alpha");
            var beta = new Contact("beta");
            bucket.Add(alpha);

            var q = bucket.TryGet(alpha.Id, out var found);
            Assert.That(q, Is.True);
            Assert.Equals(alpha, found);

            q = bucket.TryGet(beta.Id, out var notfound);
            Assert.That(q, Is.False);
            Assert.That(notfound, Is.Null);
        }

        [Test]
        public void Count()
        {
            var bucket = new KBucket<Contact>();
            Assert.Equals(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            bucket.Add(new Contact("d"));
            bucket.Add(new Contact("c"));
            bucket.Add(new Contact("d"));
            bucket.Add(new Contact("e"));
            bucket.Add(new Contact("f"));
            bucket.Add(new Contact("a"));
            Assert.Equals(6, bucket.Count);
        }

        [Test]
        public void Clear()
        {
            var bucket = new KBucket<Contact>();
            Assert.Equals(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            Assert.Equals(3, bucket.Count);

            bucket.Clear();
            Assert.Equals(0, bucket.Count);
        }

        [Test]
        public void Remove()
        {
            var bucket = new KBucket<Contact>();
            Assert.Equals(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            Assert.Equals(3, bucket.Count);

            bucket.Remove(new Contact("b"));
            Assert.Equals(2, bucket.Count);

            Assert.That(bucket.Contains(new Contact("a")), Is.True);
            Assert.That(bucket.Contains(new Contact("b")), Is.False);
            Assert.That(bucket.Contains(new Contact("c")), Is.True);
        }

        [Test]
        public void CopyTo()
        {
            var bucket = new KBucket<Contact>();
            Assert.Equals(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            Assert.Equals(3, bucket.Count);

            var array = new Contact[bucket.Count + 2];
            bucket.CopyTo(array, 1);
            Assert.That(array[0], Is.Null);
            Assert.That(array[1], Is.Not.Null);
            Assert.That(array[2], Is.Not.Null);
            Assert.That(array[3], Is.Not.Null);
            Assert.That(array[4], Is.Null);
        }

        [Test]
        public void Enumerate()
        {
            var bucket = new KBucket<Contact>();
            var nContacts = bucket.ContactsPerBucket + 1;
            for (var i = 0; i < nContacts; ++i)
            {
                bucket.Add(new Contact(i));
            }

            Assert.Equals(nContacts, bucket.Count);

            var n = 0;
            
            foreach (var _ in bucket)
            {
                ++n;
            }
            
            Assert.Equals(nContacts, n);
        }

        [Test]
        public void CanBeModified() { Assert.That(new KBucket<Contact>().IsReadOnly, Is.False); }

        [Test]
        public async Task ThreadSafe()
        {
            var bucket = new KBucket<Contact>();
            const int nContacts = 1000;
            const int nTasks = 100;
            var tasks = new Task[nTasks];

            for (var i = 0; i < nTasks; ++i)
            {
                var start = i;
                tasks[i] = Task.Run(() => AddTask(bucket, start, nContacts));
            }

            await Task.WhenAll(tasks);
        }

        public void AddTask(KBucket<Contact> bucket, int start, int count)
        {
            for (var i = 0; i < count; ++i)
            {
                bucket.Add(new Contact(start * count + i));
            }
        }
    }
}
