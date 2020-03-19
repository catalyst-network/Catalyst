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
using System.Reflection;
using System.Text.RegularExpressions;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Protocol.Network;
using Dawn;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Catalyst.TestUtils
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Property(Traits.TestType, Traits.IntegrationTest)]
    public class FileSystemBasedTest : IDisposable
    {
        protected readonly string CurrentTestName;
        public IFileSystem FileSystem;
        protected readonly TestContext Output;
        public DirectoryInfo TestDirectory { set; get; }
        protected List<string> ConfigFilesUsed { get; }
        protected readonly ContainerProvider ContainerProvider;
        private DateTime _testStartTime;

        protected FileSystemBasedTest(TestContext output, 
            IEnumerable<string> configFilesUsed = default,
            NetworkType network = default)
        {
            Guard.Argument(output, nameof(output)).NotNull();
            Output = output;
            //var currentTest = Output.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
            //   .GetValue(Output) as ITest;

            //if (currentTest == null)
            //{
            //    throw new ArgumentNullException(
            //        $"Failed to reflect current test as {nameof(ITest)} from {nameof(output)}");
            //}

            CurrentTestName = output.Test.Name;

            CreateUniqueTestDirectory();
            
            ConfigFilesUsed = new List<string>
            {
                Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
                Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(network == default ? NetworkType.Devnet : network))
            };

            configFilesUsed?.ToList().ForEach(config =>
            {
                ConfigFilesUsed.Add(config);                    
            });

            ContainerProvider = new ContainerProvider(ConfigFilesUsed, FileSystem, Output);
            ContainerProvider.ConfigureContainerBuilder(true, true);

            TestContext.WriteLine("test running in folder {0}", TestDirectory.FullName);
        }

        protected void CreateUniqueTestDirectory()
        {
            var testStartTime = DateTime.Now;
            _testStartTime = testStartTime;
            TestDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,

                //get a unique folder for this run
                CurrentTestName + $"_{_testStartTime:yyMMddHHmmssffff}"));

            TestDirectory.Exists.Should().BeFalse();
            TestDirectory.Create();

            FileSystem = GetFileSystemStub();
        }

        private IFileSystem GetFileSystemStub()
        {
            var fileSystem = Substitute.For<FileSystem>();
            fileSystem.GetCatalystDataDir().Returns(TestDirectory);
            return fileSystem;
        }

        public void Dispose() { Dispose(true); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || TestDirectory?.Parent == null)
            {
                return;
            }

            var regex = new Regex(CurrentTestName + @"_(?<timestamp>[\d]{16})");

            var oldDirectories = TestDirectory.Parent.EnumerateDirectories()
               .Where(d =>
                {
                    var matches = regex.Matches(d.Name);
                    if (matches.Count == 0) return false;
                    if (!DateTime.TryParseExact(matches[0].Groups["timestamp"].Value,
                        "yyMMddHHmmssffff", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var previousTestTimeStamp)) return false;
                    var isTestAtLeastAMinuteOld = _testStartTime.Subtract(previousTestTimeStamp) > TimeSpan.FromMinutes(1);
                    return isTestAtLeastAMinuteOld;
                })
               .ToList();
            oldDirectories.ForEach(TryDeleteFolder);
            
            ContainerProvider?.Dispose();
        }

        private static void TryDeleteFolder(DirectoryInfo d)
        {
            try
            {
                d.Delete(true);
            }
            catch (Exception)
            {
                //no big deal is this doesn't work once in a while, only worry if
                //this happens all the time.
            }
        }
    }
}
