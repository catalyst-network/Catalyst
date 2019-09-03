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
using System.Reflection;
using Dawn;

namespace Catalyst.Abstractions.Enumerator
{
    /// <inheritdoc />
    /// <summary>
    ///     <see href="https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types" />
    /// </summary>
    public class Enumeration
        : IEnumeration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        protected Enumeration(int id, string name)
        {
            Guard.Argument(name, nameof(name)).NotNull();
            Guard.Argument(id, nameof(id)).Positive();
            Id = id;
            Name = name;
        }
        
        public string Name { get; }
        public int Id { get; }
        public override string ToString() { return Name; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parsed"></param>
        /// <param name="comparison"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryParse<T>(string value,
            out T parsed,
            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase) where T : Enumeration
        {
            parsed = null;
            if (value == null)
            {
                return false;
            }

            parsed = GetAll<T>().SingleOrDefault(e => e.Name.Equals(value, comparison));
            return parsed != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="comparison"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static T Parse<T>(string value,
            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase) where T : Enumeration
        {
            Guard.Argument(value, nameof(value)).NotNull();
            var allValues = GetAll<T>();
            var enumerable = allValues as T[] ?? allValues.ToArray();
            var result = enumerable.SingleOrDefault(e => e.Name.Equals(value, comparison));
            if (result == null)
            {
                throw new FormatException($"Failed to parse {value} into a {typeof(T).Name}, " +
                    $"admitted values are {string.Join(", ", enumerable.Select(v => v.Name))}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enumeration"></param>
        /// <returns></returns>
        public static explicit operator int(Enumeration enumeration)
        {
            return enumeration.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetAll<T>() where T : IEnumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly);

            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        #region Equality members

        public bool Equals(Enumeration other) { throw new NotImplementedException(); }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((IEnumeration) obj);
        }

        public override int GetHashCode() { return Id; }
        public static bool operator ==(Enumeration left, Enumeration right) { return Equals(left, right); }
        public static bool operator !=(Enumeration left, Enumeration right) { return !Equals(left, right); }

        #endregion
    }
}
