#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Xibalbalabs.Enumeration.Core
{
    /// <summary>
    /// Base class for creating strongly-typed, <see cref="int"/>-backed enumeration classes.
    /// </summary>
    /// <typeparam name="TEnumeration">The concrete enumeration type deriving from this class.</typeparam>
    [Serializable]
    [DebuggerDisplay("{DisplayName} - {Value}")]
    public abstract class Enumeration<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        TEnumeration> : Enumeration<TEnumeration, int>
      where TEnumeration : Enumeration<TEnumeration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Enumeration{TEnumeration}"/> class.
        /// </summary>
        /// <param name="value">The underlying integer value.</param>
        /// <param name="displayName">The human-readable display name.</param>
        protected Enumeration(int value, string displayName)
            : base(value, displayName)
        {
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="Enumeration{TEnumeration, TValue}.Value"/> equals <paramref name="value"/>.
        /// </summary>
        /// <exception cref="ArgumentException">No matching instance was found.</exception>
        public static TEnumeration FromInt32(int value)
        {
            return FromValue(value);
        }

        /// <summary>
        /// Attempts to return the enumeration instance whose <see cref="Enumeration{TEnumeration, TValue}.Value"/> equals <paramref name="listItemValue"/>.
        /// </summary>
        public static bool TryFromInt32(int listItemValue, out TEnumeration? result)
        {
            return TryFromValue(listItemValue, out result);
        }

        // The operators below compare directly against the underlying int and cast to a display-name
        // string. They live here rather than on the fully generic Enumeration<TEnumeration, TValue> base
        // because TValue there could itself be a reference type (e.g. string): overloading `==`/`!=`
        // against an arbitrary TValue, or adding a second `explicit operator string`, would create
        // ambiguous-operator compile errors for every consumer of a string-backed enumeration (including
        // ordinary `someEnum == null` comparisons). int is never nullable, so these are unambiguous here.

        /// <summary>
        /// Determines whether an enumeration instance's <see cref="Enumeration{TEnumeration, TValue}.Value"/> equals <paramref name="right"/>.
        /// </summary>
        public static bool operator ==(Enumeration<TEnumeration>? left, int right)
        {
            return left is object && left.Value == right;
        }

        /// <summary>
        /// Determines whether an enumeration instance's <see cref="Enumeration{TEnumeration, TValue}.Value"/> does not equal <paramref name="right"/>.
        /// </summary>
        public static bool operator !=(Enumeration<TEnumeration>? left, int right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether <paramref name="left"/> equals an enumeration instance's <see cref="Enumeration{TEnumeration, TValue}.Value"/>.
        /// </summary>
        public static bool operator ==(int left, Enumeration<TEnumeration>? right)
        {
            return right == left;
        }

        /// <summary>
        /// Determines whether <paramref name="left"/> does not equal an enumeration instance's <see cref="Enumeration{TEnumeration, TValue}.Value"/>.
        /// </summary>
        public static bool operator !=(int left, Enumeration<TEnumeration>? right)
        {
            return !(right == left);
        }

        /// <summary>
        /// Explicitly converts an enumeration instance to its <see cref="Enumeration{TEnumeration, TValue}.DisplayName"/>.
        /// </summary>
        public static explicit operator string(Enumeration<TEnumeration> enumeration)
        {
            if (enumeration is null) throw new ArgumentNullException(nameof(enumeration));
            return enumeration.DisplayName;
        }
    }

    /// <summary>
    /// Base class for creating strongly-typed enumeration classes backed by an arbitrary <see cref="IComparable"/> value type.
    /// </summary>
    /// <typeparam name="TEnumeration">The concrete enumeration type deriving from this class.</typeparam>
    /// <typeparam name="TValue">The type of the underlying value (e.g. <see cref="int"/>, <see cref="string"/>).</typeparam>
    [Serializable]
    [DebuggerDisplay("{DisplayName} - {Value}")]
    public abstract class Enumeration<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        TEnumeration, TValue> : IComparable<TEnumeration>, IEquatable<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration, TValue>
        where TValue : IComparable
    {
        readonly string _displayName;
        readonly TValue _value;

        private static readonly Lazy<TEnumeration[]> _enumerations = new Lazy<TEnumeration[]>(GetEnumerations);

#if NET8_0_OR_GREATER
        private static readonly Lazy<FrozenDictionary<TValue, TEnumeration>> _byValue =
            new Lazy<FrozenDictionary<TValue, TEnumeration>>(() => BuildByValue().ToFrozenDictionary(EqualityComparer<TValue>.Default));

        private static readonly Lazy<FrozenDictionary<string, TEnumeration>> _byDisplayName =
            new Lazy<FrozenDictionary<string, TEnumeration>>(() => BuildByDisplayName(StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal));

        private static readonly Lazy<FrozenDictionary<string, TEnumeration>> _byDisplayNameIgnoreCase =
            new Lazy<FrozenDictionary<string, TEnumeration>>(() => BuildByDisplayName(StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
#else
        private static readonly Lazy<Dictionary<TValue, TEnumeration>> _byValue =
            new Lazy<Dictionary<TValue, TEnumeration>>(BuildByValue);

        private static readonly Lazy<Dictionary<string, TEnumeration>> _byDisplayName =
            new Lazy<Dictionary<string, TEnumeration>>(() => BuildByDisplayName(StringComparer.Ordinal));

        private static readonly Lazy<Dictionary<string, TEnumeration>> _byDisplayNameIgnoreCase =
            new Lazy<Dictionary<string, TEnumeration>>(() => BuildByDisplayName(StringComparer.OrdinalIgnoreCase));
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumeration{TEnumeration, TValue}"/> class.
        /// </summary>
        /// <param name="value">The underlying value.</param>
        /// <param name="displayName">The human-readable display name.</param>
        protected Enumeration(TValue value, string displayName)
        {
            _value = value;
            _displayName = displayName;
        }

        /// <summary>
        /// The underlying value of this enumeration instance.
        /// </summary>
        public TValue Value
        {
            get { return _value; }
        }

        /// <summary>
        /// The human-readable display name of this enumeration instance.
        /// </summary>
        public string DisplayName
        {
            get { return _displayName; }
        }

        /// <inheritdoc />
        public int CompareTo(TEnumeration? other)
        {
            return Value.CompareTo(other!.Value);
        }

        /// <inheritdoc />
        public override sealed string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Returns every declared instance of <typeparamref name="TEnumeration"/>.
        /// </summary>
        public static TEnumeration[] GetAll()
        {
            return _enumerations.Value;
        }

        private static TEnumeration[] GetEnumerations()
        {
            Type enumerationType = typeof(TEnumeration);
            return enumerationType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(info => enumerationType.IsAssignableFrom(info.FieldType))
                .Select(info => info.GetValue(null))
                .Cast<TEnumeration>()
                .ToArray();
        }

        private static Dictionary<TValue, TEnumeration> BuildByValue()
        {
            var dictionary = new Dictionary<TValue, TEnumeration>();
            foreach (var item in _enumerations.Value)
            {
                if (!dictionary.ContainsKey(item.Value))
                {
                    dictionary.Add(item.Value, item);
                }
            }
            return dictionary;
        }

        private static Dictionary<string, TEnumeration> BuildByDisplayName(IEqualityComparer<string> comparer)
        {
            var dictionary = new Dictionary<string, TEnumeration>(comparer);
            foreach (var item in _enumerations.Value)
            {
                if (!dictionary.ContainsKey(item.DisplayName))
                {
                    dictionary.Add(item.DisplayName, item);
                }
            }
            return dictionary;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as TEnumeration);
        }

        /// <inheritdoc />
        public bool Equals(TEnumeration? other)
        {
            return other != null && Value.Equals(other.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Determines whether two enumeration instances are equal by comparing their <see cref="Value"/>.
        /// </summary>
        public static bool operator ==(Enumeration<TEnumeration, TValue>? left, Enumeration<TEnumeration, TValue>? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two enumeration instances are not equal.
        /// </summary>
        public static bool operator !=(Enumeration<TEnumeration, TValue>? left, Enumeration<TEnumeration, TValue>? right)
        {
            return !Equals(left, right);
        }

        // Comparing directly against TValue (and a second `explicit operator string`) is intentionally NOT
        // offered here: TValue can be a reference type (e.g. string), and overloading `==`/`!=` against an
        // arbitrary TValue would make ordinary `someEnum == null` comparisons an ambiguous-operator compile
        // error for every consumer of such an enumeration; likewise a second cast to `string` would collide
        // with this one whenever TValue is itself string. Both are offered instead on the int-only
        // Enumeration{TEnumeration} shorthand, where TValue is always int and neither conflict can occur.

        /// <summary>
        /// Explicitly converts an enumeration instance to its underlying <typeparamref name="TValue"/>.
        /// </summary>
        public static explicit operator TValue(Enumeration<TEnumeration, TValue> enumeration)
        {
            if (enumeration is null) throw new ArgumentNullException(nameof(enumeration));
            return enumeration.Value;
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="Value"/> equals <paramref name="value"/>.
        /// </summary>
        /// <exception cref="ArgumentException">No matching instance was found.</exception>
        public static TEnumeration FromValue(TValue value)
        {
            if (TryFromValue(value, out var result))
            {
                return result!;
            }

            throw new ArgumentException(string.Format("'{0}' is not a valid value in {1}", value, typeof(TEnumeration)), "value");
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="Value"/> equals <paramref name="value"/>, or <paramref name="defaultValue"/> if none is found.
        /// </summary>
        public static TEnumeration FromValueOrDefault(TValue value, TEnumeration defaultValue)
        {
            return TryFromValue(value, out var result) ? result! : defaultValue;
        }

        /// <summary>
        /// Attempts to return the enumeration instance whose <see cref="Value"/> equals <paramref name="value"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a matching instance was found; otherwise <see langword="false"/>.</returns>
        public static bool TryFromValue(TValue value, out TEnumeration? result)
        {
            return _byValue.Value.TryGetValue(value, out result);
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="DisplayName"/> equals <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The display name to search for.</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison.</param>
        /// <exception cref="ArgumentException">No matching instance was found.</exception>
        public static TEnumeration FromName(string name, bool ignoreCase = false)
        {
            if (TryFromName(name, out var result, ignoreCase))
            {
                return result!;
            }

            throw new ArgumentException(string.Format("'{0}' is not a valid name in {1}", name, typeof(TEnumeration)), nameof(name));
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="DisplayName"/> equals <paramref name="displayName"/>.
        /// </summary>
        /// <param name="displayName">The display name to search for.</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison.</param>
        /// <exception cref="ArgumentException">No matching instance was found.</exception>
        public static TEnumeration FromDisplayName(string displayName, bool ignoreCase = false)
        {
            return FromName(displayName, ignoreCase);
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="DisplayName"/> equals <paramref name="name"/>, or <paramref name="defaultValue"/> if none is found.
        /// </summary>
        public static TEnumeration FromNameOrDefault(string name, TEnumeration defaultValue, bool ignoreCase = false)
        {
            return TryFromName(name, out var result, ignoreCase) ? result! : defaultValue;
        }

        /// <summary>
        /// Attempts to return the enumeration instance whose <see cref="DisplayName"/> equals <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The display name to search for.</param>
        /// <param name="result">The matching instance, if found.</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison.</param>
        /// <returns><see langword="true"/> if a matching instance was found; otherwise <see langword="false"/>.</returns>
        public static bool TryFromName(string name, out TEnumeration? result, bool ignoreCase = false)
        {
            if (name is null)
            {
                result = null;
                return false;
            }

            var lookup = ignoreCase ? _byDisplayNameIgnoreCase.Value : _byDisplayName.Value;
            return lookup.TryGetValue(name, out result);
        }

        /// <summary>
        /// Returns the enumeration instance whose <see cref="DisplayName"/> equals <paramref name="displayName"/>. Kept for backward compatibility with Headspring.Enumeration.
        /// </summary>
        /// <exception cref="ArgumentException">No matching instance was found.</exception>
        public static TEnumeration Parse(string displayName)
        {
            return FromName(displayName);
        }

        /// <summary>
        /// Attempts to return the enumeration instance whose <see cref="Value"/> equals <paramref name="value"/>. Kept for backward compatibility with Headspring.Enumeration.
        /// </summary>
        public static bool TryParse(TValue value, out TEnumeration result)
        {
            var found = TryFromValue(value, out var nullableResult);
            result = nullableResult!;
            return found;
        }

        /// <summary>
        /// Attempts to return the enumeration instance whose <see cref="DisplayName"/> equals <paramref name="displayName"/>. Kept for backward compatibility with Headspring.Enumeration.
        /// </summary>
        public static bool TryParse(string displayName, out TEnumeration result)
        {
            var found = TryFromName(displayName, out var nullableResult);
            result = nullableResult!;
            return found;
        }
    }
}
