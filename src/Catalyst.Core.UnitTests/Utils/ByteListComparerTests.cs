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

using System.Linq;
using System.Text;
using Catalyst.Core.Util;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Utils
{
    public class ByteListComparerTests
    {
        [Fact]
        public void ByteListComparer_should_return_0_on_same_list()
        {
            var list1 = Encoding.UTF8.GetBytes("hello");
            var list2 = Encoding.UTF8.GetBytes("hello");

            ByteUtil.ByteListMinSizeComparer.Default.Compare(list1, list2).Should().Be(0);
        }

        [Fact]
        public void ByteListComparer_should_return_0_on_same_beginning_of_list()
        {
            var list1 = Encoding.UTF8.GetBytes("hello");
            var list2 = Encoding.UTF8.GetBytes("hello").Concat(Encoding.UTF8.GetBytes("world")).ToList();

            ByteUtil.ByteListMinSizeComparer.Default.Compare(list1, list2).Should().Be(0);
        }

        [Fact]
        public void ByteListComparer_should_not_return_0_on_different_beginning_of_list()
        {
            var list1 = Encoding.UTF8.GetBytes("hello");
            var list2 = Encoding.UTF8.GetBytes("world").Concat(Encoding.UTF8.GetBytes("hello")).ToList();

            ByteUtil.ByteListMinSizeComparer.Default.Compare(list1, list2).Should().NotBe(0);
        }

        [Fact]
        public void ByteListComparer_should_return_one_on_different_on_higher_beginning_of_list()
        {
            var list1 = new byte[] {0, 1, 2, 3, 4, 5, 7, 8};
            var list2 = new byte[] {0, 1, 2, 3, 4, 5, 6, 8, 9};

            ByteUtil.ByteListMinSizeComparer.Default.Compare(list1, list2).Should().Be(1);
        }

        [Fact]
        public void ByteListComparer_should_return_minus_one_on_different_on_higher_beginning_of_list()
        {
            var list1 = new byte[] {0, 1, 2, 3, 4, 4, 7, 8};
            var list2 = new byte[] {0, 1, 2, 3, 4, 5, 6, 8};

            ByteUtil.ByteListMinSizeComparer.Default.Compare(list1, list2).Should().Be(-1);
        }

        [Fact]
        public void ByteListComparer_should_return_one_if_only_second_arg_is_null()
        {
            var list1 = new byte[] { };

            ByteUtil.ByteListMinSizeComparer.Default.Compare(list1, null).Should().Be(1);
        }

        [Fact]
        public void ByteListComparer_should_return_minus_one_if_only_first_arg_is_null()
        {
            var list2 = new byte[] {3};

            ByteUtil.ByteListMinSizeComparer.Default.Compare(null, list2).Should().Be(-1);
        }

        [Fact]
        public void ByteListComparer_should_return_0_when_both_args_are_null()
        {
            ByteUtil.ByteListMinSizeComparer.Default.Compare(null, null).Should().Be(0);
        }
    }
}
