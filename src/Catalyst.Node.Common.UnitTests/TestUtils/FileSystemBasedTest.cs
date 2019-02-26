using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Node.Common;

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
            _output = output;
            _currentTest = (_output?.GetType()
               .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
               .GetValue(_output) as ITest);
            _currentTestName = _currentTest.TestCase.TestMethod.Method.Name;
            _testDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                //get a unique folder for this run
                _currentTestName 
              + $"_{_currentTest.TestCase.TestMethodArguments?.GetHashCode() ?? 0}"
              + $"_{DateTime.Now:yyMMddHHmmssff}"));

            _testDirectory.Exists.Should().BeFalse();
            _testDirectory.Create();

            _fileSystem = Substitute.For<IFileSystem>();
            _fileSystem.GetCatalystHomeDir().Returns(_testDirectory);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var regex = new Regex(_currentTestName + @"_([\d]{14})");
                var oldDirectories = _testDirectory.Parent.EnumerateDirectories()
                   .Where(d =>
                    {
                        return _testDirectory.Name != d.Name
                         && regex.IsMatch(d.Name);
                    })
                   .ToList();

                oldDirectories
                   .ForEach(d => d.Delete(true));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
