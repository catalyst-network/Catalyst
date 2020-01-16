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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.FileSystem;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.Migration
{
    class MigrateTo1 : IMigration
    {
        class Pin1
        {
            public Cid Id;
        }

        public int Version => 1;

        public bool CanUpgrade => true;

        public bool CanDowngrade => true;

        public async Task DowngradeAsync(RepositoryOptions options, CancellationToken cancel = default(CancellationToken))
        {
            var path = Path.Combine(options.Folder, "pins");
            var folder = new DirectoryInfo(path);
            if (!folder.Exists)
            {
                return;
            }

            var store = new FileStore<Cid, Pin1>
            {
                Folder = path,
                NameToKey = (cid) => cid.Hash.ToBase32(),
                KeyToName = (key) => new MultiHash(Base32.FromBase32(key))
            };

            var files = folder.EnumerateFiles().Where(fi => fi.Length != 0);
            foreach (var fi in files)
            {
                try
                {
                    var name = store.KeyToName(fi.Name);
                    var pin = await store.GetAsync(name, cancel).ConfigureAwait(false);
                    File.Create(Path.Combine(store.Folder, pin.Id));
                    File.Delete(store.GetPath(name));
                }
                catch { }
            }
        }

        public async Task UpgradeAsync(RepositoryOptions options, CancellationToken cancel = default(CancellationToken))
        {
            var path = Path.Combine(options.Folder, "pins");
            var folder = new DirectoryInfo(path);
            if (!folder.Exists)
            {
                return;
            }

            var store = new FileStore<Cid, Pin1>
            {
                Folder = path,
                NameToKey = (cid) => cid.Hash.ToBase32(),
                KeyToName = (key) => new MultiHash(Base32.FromBase32(key))
            };

            var files = folder.EnumerateFiles().Where(fi => fi.Length == 0);
            foreach (var fi in files)
            {
                try
                {
                    var cid = Cid.Decode(fi.Name);
                    await store.PutAsync(cid, new Pin1
                    {
                        Id = cid
                    }, cancel).ConfigureAwait(false);
                    File.Delete(fi.FullName);
                }
                catch { }
            }
        }
    }
}
