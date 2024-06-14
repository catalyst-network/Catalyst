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
using System.Linq;
using Lib.P2P.Routing;
using MultiFormats;

namespace Lib.P2P.Tests.Routing
{
    public class ContentRouterTest
    {
        private Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            Addresses = new[]
            {
                new MultiAddress("/ip4/127.0.0.1/tcp/4001")
            }
        };

        private Cid cid1 =
            "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";

        [Test]
        public void Add()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id);

                var providers = router.Get(cid1);
                Assert.That(providers.Count(), Is.EqualTo(1));
                Assert.That(providers.First(), Is.EqualTo(self.Id));
            }
        }

        [Test]
        public void Add_Duplicate()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id);
                router.Add(cid1, self.Id);

                var providers = router.Get(cid1);
                Assert.That(1, Is.EqualTo(providers.Count()));
                Assert.That(self.Id, Is.EqualTo(providers.First()));
            }
        }

        [Test]
        public void Add_MultipleProviders()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id);
                router.Add(cid1, other.Id);

                var providers = router.Get(cid1).ToArray();
                Assert.That(2, Is.EqualTo(providers.Length));
                Assert.That(providers, Contains.Item(self.Id));
                Assert.That(providers, Contains.Item(other.Id));
            }
        }

        [Test]
        public void Get_NonexistentCid()
        {
            using (var router = new ContentRouter())
            {
                var providers = router.Get(cid1);
                Assert.That(0, Is.EqualTo(providers.Count()));
            }
        }

        [Test]
        public void Get_Expired()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id, DateTime.MinValue);

                var providers = router.Get(cid1);
                Assert.That(0, Is.EqualTo(providers.Count()));
            }
        }

        [Test]
        public void Get_NotExpired()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id, DateTime.MinValue);
                var providers = router.Get(cid1);
                Assert.That(0, Is.EqualTo(providers.Count()));

                router.Add(cid1, self.Id, DateTime.MaxValue - router.ProviderTtl);
                providers = router.Get(cid1);
                Assert.That(1, Is.EqualTo(providers.Count()));
            }
        }
    }
}
