using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Common.UnitTests.TestUtils
{
    /// <summary>
    /// A base test class that can be used to offer inheriting tests a folder on which
    /// to create files, logs, etc.
    /// </summary>
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public abstract class FileSystemBasedTest : IDisposable
    {
        protected readonly ITest _currentTest;
        protected readonly string _currentTestName;
        protected readonly ITestOutputHelper _output;
        protected readonly IFileSystem _fileSystem;
        private DirectoryInfo _testDirectory;

        public FileSystemBasedTest(ITestOutputHelper output)
        {
            var stacktrace = Environment.StackTrace;
            _output = output;
            _currentTest = (_output?.GetType()
               .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
               .GetValue(_output) as ITest);
            _currentTestName = _currentTest.TestCase.TestMethod.Method.Name;
            var testStartTime = DateTime.Now;
            _testDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                //get a unique folder for this run
                _currentTestName + $"_{testStartTime:yyMMddHHmmssffff}"));

            _testDirectory.Exists.Should().BeFalse();
            _testDirectory.Create();

            _fileSystem = Substitute.For<IFileSystem>();
            _fileSystem.GetCatalystHomeDir().Returns(_testDirectory);

            _output.WriteLine("test running in folder {0}", _testDirectory.FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            var regex = new Regex(_currentTestName + @"_(?<timestamp>[\d]{14})");
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

        public void Dispose()
        {
            Dispose(true);
        }

        public static uint GetHashFromItems<T>(IEnumerable<T> items)
        {
            if (items == null) return 0;
            unchecked
            {
                var hash = 19;
                foreach (var obj in items)
                {
                    hash = hash * 31 + obj.GetHashCode();
                }
                return (uint)hash;
            }
        }
    }
}
