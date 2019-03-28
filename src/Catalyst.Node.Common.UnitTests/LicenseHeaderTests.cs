/*
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

using System;
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
        private const string LicenseHeaderFileName = "LicenseHeader.txt";

        [Fact]
        public async Task All_Cs_Files_Should_Have_License_Header()
        {
            var declaringTypeAssembly = MethodBase.GetCurrentMethod().DeclaringType.Assembly;

            var basePathRegex = new Regex("(?<sourcePath>(.*)[\\/]src[\\/])(.*)");
            var codeBasePath = new Uri(declaringTypeAssembly.CodeBase).AbsolutePath;
            var sourcePath = basePathRegex.Match(codeBasePath).Groups["sourcePath"].Value;

            sourcePath.Should().NotBeNull();
            var sourceDirectory = new DirectoryInfo(sourcePath);

            var licenseText = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, LicenseHeaderFileName));
            licenseText.Should().StartWith(@"/*");


            var getWrongFiles = sourceDirectory.EnumerateFiles("*.cs", SearchOption.AllDirectories)
               .Select(async f =>
                {
                    if (f.Name.EndsWith("AssemblyInfo.cs")) { return null;}
                    var allText = await File.ReadAllTextAsync(f.FullName);
                    return allText.StartsWith(licenseText) ? null : f.FullName;
                }).ToArray();

            var files = (await Task.WhenAll(getWrongFiles)).Where(f => f != null).ToArray();

            files.Should().BeEmpty(
                $"all files should have a header{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, files)} " + Environment.NewLine);
        }
    }
}
