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

namespace Catalyst.Common.UnitTests.TestUtils
{
    public static class FluentAssertionsExtensions
    {
        private static AndConstraint<GenericCollectionAssertions<T>> NotBeInOrder<T, TSelector>(this GenericCollectionAssertions<T> constraint,
            Expression<Func<T, TSelector>> propertyExpression,
            IComparer<TSelector> comparer,
            int expectedComparisonResult)
        {
            var enumerable = constraint.Subject;
            if (!enumerable.Any()) return new AndConstraint<GenericCollectionAssertions<T>>(constraint);
            var currentItem = enumerable.First();
            foreach (var item in enumerable.Skip(1))
            {
                var currentProperty = propertyExpression.Compile()(currentItem);
                var nextProperty = propertyExpression.Compile()(item);
                if (comparer.Compare(currentProperty, nextProperty) == expectedComparisonResult)
                {
                    return new AndConstraint<GenericCollectionAssertions<T>>(constraint);
                }
                else
                {
                    currentItem = item;
                }
            }

            throw new AssertionFailedException("Expected items not to be sorted in descending order but they are.");
        }

        public static AndConstraint<GenericCollectionAssertions<T>> NotBeInDescendingOrder<T, TSelector>(this GenericCollectionAssertions<T> constraint,
            Expression<Func<T, TSelector>> propertyExpression,
            IComparer<TSelector> comparer)
        {
            return NotBeInOrder(constraint, propertyExpression, comparer, 1);
        }

        public static AndConstraint<GenericCollectionAssertions<T>> NotBeInAscendingOrder<T, TSelector>(this GenericCollectionAssertions<T> constraint,
            Expression<Func<T, TSelector>> propertyExpression,
            IComparer<TSelector> comparer)
        {
            return NotBeInOrder(constraint, propertyExpression, comparer, -1);
        }
    }
}
