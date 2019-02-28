﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawn;

namespace Catalyst.Node.Common.Helpers
{
    /// <summary>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types"/>
    /// </summary>
    public abstract class Enumeration : IComparable
    {
        public string Name { get; }
        public int Id { get; }

        protected Enumeration() { }

        protected Enumeration(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString() => Name;

        public static bool TryParse<T>(string value, out T parsed, 
            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase) where T : Enumeration
        {
            parsed = null;
            if (value == null) return false;
            parsed = GetAll<T>().SingleOrDefault(e => e.Name.Equals(value, comparison));
            return parsed != null;
        }

        public static T Parse<T>(string value,
            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase) where T : Enumeration
        {
            Guard.Argument(value, nameof(value)).NotNull();
            var allValues = GetAll<T>();
            var result = allValues.SingleOrDefault(e => e.Name.Equals(value, comparison));
            if(result == null) throw new FormatException($"Failed to parse {value} into a {typeof(T).Name}, " +
                $"admitted values are {string.Join(", ", allValues.Select(v => v.Name))}");
            return result;
        }

        public static IEnumerable<T> GetAll<T>() where T : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly);

            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public override bool Equals(object obj)
        {
            var otherValue = obj as Enumeration;

            if (otherValue == null)
                return false;

            var typeMatches = GetType().Equals(obj.GetType());
            var valueMatches = Id.Equals(otherValue.Id);

            return typeMatches && valueMatches;
        }

        protected bool Equals(Enumeration other)
        {
            return string.Equals(Name, other.Name) && Id == other.Id;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Id;
            }
        }

        public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);
    }
}
