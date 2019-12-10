using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.Options;
using Common.Logging;

namespace Catalyst.Core.Modules.Dfs.Migration
{
    /// <summary>
    ///   Allows migration of the repository. 
    /// </summary>
    public sealed class MigrationManager : IMigrationManager
    {
        private readonly RepositoryOptions _options;
        private static readonly ILog Log = LogManager.GetLogger(typeof(MigrationManager));

        /// <summary>
        ///   Creates a new instance of the <see cref="MigrationManager"/> class
        ///   for the specifed <see cref="DfsService"/>.
        /// </summary>
        public MigrationManager(RepositoryOptions options)
        {
            _options = options;
            Migrations = typeof(MigrationManager).Assembly.GetTypes()
               .Where(x => typeof(IMigration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
               .Select(x => (IMigration) Activator.CreateInstance(x))
               .OrderBy(x => x.Version)
               .ToList();
        }

        /// <summary>
        ///   The list of migrations that can be performed.
        /// </summary>
        public List<IMigration> Migrations { get; private set; }

        /// <summary>
        ///   Gets the latest supported version number of a repository.
        /// </summary>
        public int LatestVersion => Migrations.Last().Version;

        /// <summary>
        ///   Gets the current vesion number of the  repository.
        /// </summary>
        public int CurrentVersion
        {
            get
            {
                var path = VersionPath();
                if (!File.Exists(path))
                {
                    return 0;
                }
                
                using (var reader = new StreamReader(path))
                {
                    var s = reader.ReadLine();
                    return int.Parse(s ?? throw new NullReferenceException(("stream null")));
                }
            }
            private set => File.WriteAllText(VersionPath(), value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///   Upgrade/downgrade to the specified version.
        /// </summary>
        /// <param name="version">
        ///   The required version of the repository.
        /// </param>
        /// <param name="cancel">
        /// </param>
        /// <returns></returns>
        public async Task MirgrateToVersionAsync(int version, CancellationToken cancel = default)
        {
            if (version != 0 && Migrations.All(m => m.Version != version))
            {
                throw new ArgumentOutOfRangeException(nameof(version), $@"Repository version '{version.ToString()}' is unknown.");
            }

            var currentVersion = CurrentVersion;
            var increment = CurrentVersion < version ? 1 : -1;
            while (currentVersion != version)
            {
                var nextVersion = currentVersion + increment;
                Log.InfoFormat("Migrating to version {0}", nextVersion.ToString());

                if (increment > 0)
                {
                    var migration = Migrations.FirstOrDefault(m => m.Version == nextVersion);
                    if (migration != null && migration.CanUpgrade)
                    {
                        await migration.UpgradeAsync(_options, cancel);
                    }
                }
                else if (increment < 0)
                {
                    var migration = Migrations.FirstOrDefault(m => m.Version == currentVersion);
                    if (migration != null && migration.CanDowngrade)
                    {
                        await migration.DowngradeAsync(_options, cancel);
                    }
                }

                CurrentVersion = nextVersion;
                currentVersion = nextVersion;
            }
        }

        /// <summary>
        ///   Gets the FQN of the version file.
        /// </summary>
        /// <returns>
        ///   The path to the version file.
        /// </returns>
        private string VersionPath()
        {
            return Path.Combine(_options.ExistingFolder(), "version");
        }
    }
}
