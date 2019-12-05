using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    [TestClass]
    public class CollectionTest
    {
        [TestMethod]
        public void Add()
        {
            var bucket = new KBucket<Contact>();
            var x = new Contact("1");
            bucket.Add(x);
            Assert.AreEqual(1, bucket.Count);
            Assert.IsTrue(bucket.Contains(x));
        }

        [TestMethod]
        public void AddDuplicate()
        {
            var bucket = new KBucket<Contact>();
            var x = new Contact("1");
            bucket.Add(x);
            bucket.Add(x);
            Assert.AreEqual(1, bucket.Count);
            Assert.IsTrue(bucket.Contains(x));
        }

        [TestMethod]
        public void AddBadContact()
        {
            var bucket = new KBucket<Contact>();
            ExceptionAssert.Throws<ArgumentNullException>(() => bucket.Add(null));
            ExceptionAssert.Throws<ArgumentNullException>(() => bucket.Add(new Contact("a") {Id = null}));
            ExceptionAssert.Throws<ArgumentNullException>(() => bucket.Add(new Contact("a") {Id = new byte[0]}));
        }

        [TestMethod]
        public void TryGet()
        {
            var bucket = new KBucket<Contact>();
            var alpha = new Contact("alpha");
            var beta = new Contact("beta");
            bucket.Add(alpha);

            var q = bucket.TryGet(alpha.Id, out var found);
            Assert.IsTrue(q);
            Assert.AreSame(alpha, found);

            q = bucket.TryGet(beta.Id, out var notfound);
            Assert.IsFalse(q);
            Assert.IsNull(notfound);
        }

        [TestMethod]
        public void Count()
        {
            var bucket = new KBucket<Contact>();
            Assert.AreEqual(0, bucket.Count);

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
            Assert.AreEqual(6, bucket.Count);
        }

        [TestMethod]
        public void Clear()
        {
            var bucket = new KBucket<Contact>();
            Assert.AreEqual(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            Assert.AreEqual(3, bucket.Count);

            bucket.Clear();
            Assert.AreEqual(0, bucket.Count);
        }

        [TestMethod]
        public void Remove()
        {
            var bucket = new KBucket<Contact>();
            Assert.AreEqual(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            Assert.AreEqual(3, bucket.Count);

            bucket.Remove(new Contact("b"));
            Assert.AreEqual(2, bucket.Count);

            Assert.IsTrue(bucket.Contains(new Contact("a")));
            Assert.IsFalse(bucket.Contains(new Contact("b")));
            Assert.IsTrue(bucket.Contains(new Contact("c")));
        }

        [TestMethod]
        public void CopyTo()
        {
            var bucket = new KBucket<Contact>();
            Assert.AreEqual(0, bucket.Count);

            bucket.Add(new Contact("a"));
            bucket.Add(new Contact("b"));
            bucket.Add(new Contact("c"));
            Assert.AreEqual(3, bucket.Count);

            var array = new Contact[bucket.Count + 2];
            bucket.CopyTo(array, 1);
            Assert.IsNull(array[0]);
            Assert.IsNotNull(array[1]);
            Assert.IsNotNull(array[2]);
            Assert.IsNotNull(array[3]);
            Assert.IsNull(array[4]);
        }

        [TestMethod]
        public void Enumerate()
        {
            var bucket = new KBucket<Contact>();
            var nContacts = bucket.ContactsPerBucket + 1;
            for (var i = 0; i < nContacts; ++i) bucket.Add(new Contact(i));
            Assert.AreEqual(nContacts, bucket.Count);

            var n = 0;
            foreach (var contact in bucket) ++n;
            Assert.AreEqual(nContacts, n);
        }

        [TestMethod]
        public void CanBeModified() { Assert.IsFalse(new KBucket<Contact>().IsReadOnly); }

        [TestMethod]
        public async Task ThreadSafe()
        {
            var bucket = new KBucket<Contact>();
            var nContacts = 1000;
            var nTasks = 100;
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
            for (var i = 0; i < count; ++i) bucket.Add(new Contact(start * count + i));
        }
    }
}
