namespace Cloudtoid
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using static Contract;

    public static class CollectionExtensions
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty<T>(this ICollection<T> value) => value.Count == 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty<T>(this IReadOnlyCollection<T> value) => value.Count == 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty<T>(this T[] value) => value.Length == 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this ICollection<T>? value) => value is null || value.Count == 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T>? value) => value is null || value.Count == 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this T[]? value) => value is null || value.Length == 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<T>? AsReadOnlyListOrDefault<T>(this IEnumerable<T>? items) => items?.AsReadOnlyList();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<T>? AsListOrDefault<T>(this IEnumerable<T>? items) => items?.AsList();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> items)
        {
            CheckValue(items, nameof(items));

            // Do NOT type check with ICollection<T>. ToArray<T>() already does that type check internally.
            // Also, arrays and lists implement both ICollection<T> and IReadOnlyCollection<T>. Given these are the most common
            // implementations of these interfaces, we don't need to check IReadOnlyCollection<T> explicitly.

            return items as IReadOnlyList<T> ?? items.ToArray();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<T> AsList<T>(this IEnumerable<T> items)
        {
            CheckValue(items, nameof(items));
            return items as IList<T> ?? items.ToList();
        }

        [DebuggerStepThrough]
        public static void AddRange<TKey, TValue>(this ICollection<KeyValuePair<TKey, TValue>> destination, IEnumerable<KeyValuePair<TKey, TValue>>? source)
        {
            CheckValue(destination, nameof(destination));

            if (source is null)
                return;

            foreach (var kvp in source)
                destination.Add(kvp);
        }
    }
}