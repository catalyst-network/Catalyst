using System;
using System.Collections.Generic;
using System.Globalization;
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
        private DateTime _testStartTime;

        public FileSystemBasedTest(ITestOutputHelper output)
        {
            _output = output;
            _currentTest = (_output?.GetType()
               .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
               .GetValue(_output) as ITest);
            _currentTestName = _currentTest.TestCase.TestMethod.Method.Name;
            _testStartTime = DateTime.Now;
            var testCaseTestMethodArguments = _currentTest.TestCase.TestMethodArguments;
                //?? _currentTest.TestCase.TestMethod.Method.GetParameters()
            _testDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                //get a unique folder for this run
                _currentTestName 
              //+ $"_{GetHashFromItems(testCaseTestMethodArguments):D10}"
              + $"_{Guid.NewGuid().ToString().Replace("-", "").Substring(0,10)}"
              + $"_{_testStartTime:yyMMddHHmmssff}"));

            _testDirectory.Exists.Should().BeFalse();
            _testDirectory.Create();

            _fileSystem = Substitute.For<IFileSystem>();
            _fileSystem.GetCatalystHomeDir().Returns(_testDirectory);

            _output.WriteLine("test running in folder {0}", _testDirectory.FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var regex = new Regex(_currentTestName + @"_(?<timestamp>[\d]{14})");
                var oldDirectories = _testDirectory.Parent.EnumerateDirectories()
                   .Where(d => DirectoryIsForATestRunOlderThanAMinute(d.Name, regex))
                   .ToList();
                oldDirectories.ForEach(TryDeleteFolder);
            }
        }

        private bool DirectoryIsForATestRunOlderThanAMinute(string directoryName, Regex regex)
        {
            var isDirectoryForTheCurrentRun = _testDirectory.Name != directoryName;
            if (isDirectoryForTheCurrentRun) return false;

            try
            {
                var match = regex.Match(directoryName);
                var oldFolderTimeStamp = DateTime.ParseExact(match.Groups["timestamp"].Value, "yyMMddHHmmssff",
                    CultureInfo.InvariantCulture);
                
                var isDirectoryForAPreviousRunOfThisTest = match.Success
                    && _testStartTime.CompareTo(oldFolderTimeStamp.AddMinutes(1)) > 0;

                return isDirectoryForAPreviousRunOfThisTest;
            }
            catch (Exception)
            {
                return false;
            }
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
            GC.SuppressFinalize(this);
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
