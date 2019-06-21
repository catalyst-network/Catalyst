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
using Catalyst.Common.Interfaces.FileSystem;
using Dawn;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using Catalyst.TestUtils;

namespace Catalyst.Common.IntegrationTests.IO
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public abstract class FileSystemTest : IDisposable
    {
        protected readonly ITest CurrentTest;
        protected readonly string CurrentTestName;
        protected readonly IFileSystem FileSystem;
        protected readonly ITestOutputHelper Output;
        private readonly DirectoryInfo _testDirectory;

        protected FileSystemTest(ITestOutputHelper output)
        {
            Guard.Argument(output, nameof(output)).NotNull();
            Output = output;
            CurrentTest = Output.GetType()
               .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
               .GetValue(Output) as ITest;

            if (CurrentTest == null)
            {
                throw new ArgumentNullException(
                    $"Failed to reflect current test as {nameof(ITest)} from {nameof(output)}");
            }

            CurrentTestName = CurrentTest.TestCase.TestMethod.Method.Name;
            var testStartTime = DateTime.Now;
            _testDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,

                //get a unique folder for this run
                CurrentTestName + $"_{testStartTime:yyMMddHHmmssffff}"));

            _testDirectory.Exists.Should().BeFalse();
            _testDirectory.Create();

            FileSystem = Substitute.For<IFileSystem>();
            FileSystem.GetCatalystDataDir().Returns(_testDirectory);

            Output.WriteLine("test running in folder {0}", _testDirectory.FullName);
        }

        public void Dispose() { Dispose(true); }

        protected virtual void Dispose(bool disposing)
        {
            //if (!disposing || _testDirectory.Parent == null)
            //{
            //    return;
            //}

            //var regex = new Regex(CurrentTestName + @"_(?<timestamp>[\d]{14})");

            //var oldDirectories = _testDirectory.Parent.EnumerateDirectories()
            //   .Where(d => regex.IsMatch(d.Name)
            //     && string.CompareOrdinal(d.Name, _testDirectory.Name) == -1)
            //   .ToList();
            //oldDirectories.ForEach(TryDeleteFolder);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void ConfigFilePointer_Should_Be_Created_As_It_Does_Not_Exist()
        {
            //var myIp = await Ip.GetPublicIpAsync();
            //myIp.Should().NotBe(default(IPAddress));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void ConfigFilePointer_Should_Not_Get_Created_As_It_Already_Exists()
        {
           // var myIp = await Ip.GetPublicIpAsync();
            //myIp.Should().NotBe(default(IPAddress));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Get_Data_Directory_Should_Successfully_Read_ConfigPointerFile()
        {
            // var myIp = await Ip.GetPublicIpAsync();
            //myIp.Should().NotBe(default(IPAddress));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Get_Data_Directory_Should_Handle_NonExistant_File_Location()
        {
            // var myIp = await Ip.GetPublicIpAsync();
            //myIp.Should().NotBe(default(IPAddress));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Should_Not_Use_ConfigFilePointer()
        {
            // var myIp = await Ip.GetPublicIpAsync();
            //myIp.Should().NotBe(default(IPAddress));
        }
    }
}
