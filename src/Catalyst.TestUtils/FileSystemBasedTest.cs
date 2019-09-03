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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Core.FileSystem;
using Dawn;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.TestUtils
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public class FileSystemBasedTest : IDisposable
    {
        protected readonly string CurrentTestName;
        protected IFileSystem FileSystem;
        protected readonly ITestOutputHelper Output;
        private DirectoryInfo _testDirectory;

        protected FileSystemBasedTest(ITestOutputHelper output)
        {
            Guard.Argument(output, nameof(output)).NotNull();
            Output = output;
            var currentTest = Output.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
               .GetValue(Output) as ITest;

            if (currentTest == null)
            {
                throw new ArgumentNullException(
                    $"Failed to reflect current test as {nameof(ITest)} from {nameof(output)}");
            }

            CurrentTestName = currentTest.TestCase.TestMethod.Method.Name;

            GenerateConfigFilesDirectory();

            Output.WriteLine("test running in folder {0}", _testDirectory.FullName);
        }

        protected void GenerateConfigFilesDirectory()
        {
            var testStartTime = DateTime.Now;
            _testDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,

                //get a unique folder for this run
                CurrentTestName + $"_{testStartTime:yyMMddHHmmssffff}"));

            _testDirectory.Exists.Should().BeFalse();
            _testDirectory.Create();

            FileSystem = GetFileSystemStub();
        }

        private IFileSystem GetFileSystemStub()
        {
            var fileSystem = Substitute.ForPartsOf<FileSystem>();
            fileSystem.GetCatalystDataDir().Returns(_testDirectory);
            return fileSystem;
        }

        public void Dispose() { Dispose(true); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _testDirectory?.Parent == null)
            {
                return;
            }

            var regex = new Regex(CurrentTestName + @"_(?<timestamp>[\d]{14})");

            var oldDirectories = _testDirectory.Parent.EnumerateDirectories()
               .Where(d => regex.IsMatch(d.Name)
                 && string.CompareOrdinal(d.Name, _testDirectory.Name) == -1)
               .ToList();
            oldDirectories.ForEach(TryDeleteFolder);
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
