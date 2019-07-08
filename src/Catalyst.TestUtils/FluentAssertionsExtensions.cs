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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using Xunit;

namespace Catalyst.TestUtils
{
    public static class FluentAssertionsExtensions
    {
        private static AndConstraint<GenericCollectionAssertions<T>> NotBeInOrder<T, TSelector>(this GenericCollectionAssertions<T> constraint,
            Expression<Func<T, TSelector>> propertyExpression,
            IComparer<TSelector> comparer,
            int resultWhenNotSorted,
            string errorMessage)
        {
            var enumerable = constraint.Subject as T[] ?? constraint.Subject.ToArray();

            if (!enumerable.Any())
            {
                return new AndConstraint<GenericCollectionAssertions<T>>(constraint);
            }

            var currentItem = enumerable.First();
            foreach (var item in enumerable.Skip(1))
            {
                var currentProperty = propertyExpression.Compile()(currentItem);
                var nextProperty = propertyExpression.Compile()(item);
                var compare = comparer.Compare(currentProperty, nextProperty);
                
                if (compare != resultWhenNotSorted)
                {
                    currentItem = item;
                    continue;
                }

                return new AndConstraint<GenericCollectionAssertions<T>>(constraint);
            }

            throw new AssertionFailedException(string.Format(errorMessage, string.Join(",", enumerable)));
        }

        public static AndConstraint<GenericCollectionAssertions<T>> NotBeInDescendingOrder<T, TSelector>(this GenericCollectionAssertions<T> constraint,
            Expression<Func<T, TSelector>> propertyExpression,
            IComparer<TSelector> comparer)
        {
            return NotBeInOrder(constraint, propertyExpression, comparer, -1, 
                "Expected items not to be sorted in descending order but they are: [{0}]");
        }

        public static AndConstraint<GenericCollectionAssertions<T>> NotBeInAscendingOrder<T, TSelector>(this GenericCollectionAssertions<T> constraint,
            Expression<Func<T, TSelector>> propertyExpression,
            IComparer<TSelector> comparer)
        {
            return NotBeInOrder(constraint, propertyExpression, comparer, 1, 
                "Expected items not to be sorted in ascending order but they are: [{0}]");
        }
    }

    public sealed class FluentAssertionsExtensionsTests
    {
        private sealed class StringWrapper
        {
            internal StringWrapper(string stringValue) { StringValue = stringValue; }
            internal string StringValue { get; }
        }

        private readonly StringWrapper[] _stringsInAscendingOrder;
        private readonly IEnumerable<StringWrapper> _stringsInDescendingOrder;
        private readonly IEnumerable<StringWrapper> _stringsInRandomOrder;

        public FluentAssertionsExtensionsTests()
        {
            _stringsInAscendingOrder = new[] {"A", "a", "b", "d", "r", "Z"}
               .Select(x => new StringWrapper(x)).ToArray();
            _stringsInDescendingOrder = _stringsInAscendingOrder.Reverse();
            _stringsInRandomOrder = new[] {"A", "a", "Z", "b", "r", "d"}
               .Select(x => new StringWrapper(x));
        }

        [Fact]
        public void NotBeInDescendingOrder_should_only_throw_when_in_descending_order()
        {
            _stringsInAscendingOrder.Should()
               .NotBeInDescendingOrder(x => x.StringValue, StringComparer.InvariantCultureIgnoreCase);
            _stringsInRandomOrder.Should()
               .NotBeInDescendingOrder(x => x.StringValue, StringComparer.InvariantCultureIgnoreCase);
            new Action(() => _stringsInDescendingOrder.Should()
                   .NotBeInDescendingOrder(x => x.StringValue, StringComparer.InvariantCultureIgnoreCase))
               .Should().Throw<AssertionFailedException>();
        }

        [Fact]
        public void NotBeInDescendingOrder_should_use_the_passed_in_comparer()
        {
            new Action(() => _stringsInDescendingOrder.Should()
                   .NotBeInDescendingOrder(x => x.StringValue, 
                        StringComparer.InvariantCultureIgnoreCase))
               .Should().Throw<AssertionFailedException>();

            _stringsInDescendingOrder.Should()
               .NotBeInDescendingOrder(x => x.StringValue, 
                    StringComparer.InvariantCulture);
        }

        [Fact]
        public void NotBeInAscendingOrder_should_only_throw_when_in_ascending_order()
        {
            _stringsInDescendingOrder.Should()
               .NotBeInAscendingOrder(x => x.StringValue, StringComparer.InvariantCultureIgnoreCase);
            _stringsInRandomOrder.Should()
               .NotBeInAscendingOrder(x => x.StringValue, StringComparer.InvariantCultureIgnoreCase);
            new Action(() => _stringsInAscendingOrder.Should()
                   .NotBeInAscendingOrder(x => x.StringValue, StringComparer.InvariantCultureIgnoreCase))
               .Should().Throw<AssertionFailedException>();
        }

        [Fact]
        public void NotBeInAscendingOrder_should_use_the_passed_in_comparer()
        {
            new Action(() => _stringsInAscendingOrder.Should()
                   .NotBeInAscendingOrder(x => x.StringValue, 
                        StringComparer.InvariantCultureIgnoreCase))
               .Should().Throw<AssertionFailedException>();

            _stringsInAscendingOrder.Should()
               .NotBeInAscendingOrder(x => x.StringValue, 
                    StringComparer.InvariantCulture);
        }

        [Fact]
        public void NotBeIn_ascending_or_descending_Order_should_be_true_on_empty()
        {
            new List<StringWrapper>().Should()
               .NotBeInAscendingOrder(x => x.StringValue,
                    StringComparer.InvariantCulture);

            new List<StringWrapper>().Should()
               .NotBeInDescendingOrder(x => x.StringValue,
                    StringComparer.InvariantCulture);
        }
    }
}
