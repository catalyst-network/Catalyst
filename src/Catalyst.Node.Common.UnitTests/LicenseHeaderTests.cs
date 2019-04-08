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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Catalyst.Node.Common.UnitTests.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Common.UnitTests
{
    public class LicenseHeaderTests : FileSystemBasedTest
    {
        public LicenseHeaderTests(ITestOutputHelper output) : base(output) { }
        private const string LicenseHeaderFileName = "COPYING";

        [Fact]
        public static async Task All_Cs_Files_Should_Have_License_Header()
        {
            var declaringTypeAssembly = MethodBase.GetCurrentMethod().DeclaringType.Assembly;

            var basePathRegex = new Regex("(?<sourcePath>(.*)[\\/]src[\\/])(.*)");
            var codeBasePath = new Uri(declaringTypeAssembly.CodeBase).AbsolutePath;
            var sourcePath = basePathRegex.Match(codeBasePath).Groups["sourcePath"].Value;

            sourcePath.Should().NotBeNull();
            var sourceDirectory = new DirectoryInfo(sourcePath);

            var copyingText = (await File.ReadAllTextAsync(Path.Combine(sourceDirectory.Parent?.FullName, LicenseHeaderFileName)))
               .TrimEndOfLines();
            copyingText.Should().StartWith(@"#region LICENSE");

            var getWrongFiles = sourceDirectory.EnumerateFiles("*.cs", SearchOption.AllDirectories)
               .Where(f => f.Directory?.Parent?.Parent?.Name != "obj")
               .Select(async f =>
                {
                    var allText = (await File.ReadAllTextAsync(f.FullName)).TrimEndOfLines();
                    return allText.StartsWith(copyingText) ? null : f.FullName;
                }).ToArray();

            var wrongFiles = (await Task.WhenAll(getWrongFiles)).Where(f => f != null).ToArray();

            wrongFiles.Should().BeEmpty(
                $"all files should have a header{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, wrongFiles)} " + Environment.NewLine);
        }
    }

    internal static class StringExtensions
    {
        internal static string TrimEndOfLines(this string originalString)
        {
            return new string(originalString.Where(c => c != '\n' && c != '\r').ToArray());
        }
    }
}
