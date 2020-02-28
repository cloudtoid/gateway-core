namespace Cloudtoid
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class EnumerableExtensions
    {
        [DebuggerStepThrough]
        public static IEnumerable<TItem> Concat<TItem>(this IEnumerable<TItem>? items, TItem item) => items.ConcatOrEmpty(new[] { item });

        /// <summary>
        /// It safely concatenates two enumerables. If the enumerables are null, they are treated as empty.
        /// </summary>
        [DebuggerStepThrough]
        public static IEnumerable<TItem> ConcatOrEmpty<TItem>(this IEnumerable<TItem>? first, IEnumerable<TItem>? second)
        {
            if (second is null)
            {
                if (first is null)
                    return Array.Empty<TItem>();

                return first;
            }

            if (first is null)
                return second;

            return Enumerable.Concat(first, second);
        }

        [DebuggerStepThrough]
        public static IEnumerable<TItem> WhereNotNull<TItem>(this IEnumerable<TItem?> items) where TItem : class
        {
            var result = items.Where(i => !(i is null));
            return result!;
        }

        [DebuggerStepThrough]
        public static IEnumerable<TItem> WhereNotNull<TItem>(this IEnumerable<TItem?> items) where TItem : struct
        {
            return items.Where(i => i.HasValue).Select(i => i!.Value);
        }

        [DebuggerStepThrough]
        public static int IndexOf<TItem>(this IEnumerable<TItem> items, TItem item, IEqualityComparer<TItem>? comparer = null)
        {
            comparer ??= EqualityComparer<TItem>.Default;

            int i = 0;
            foreach (var it in items)
            {
                if (comparer.Equals(item, it))
                    return i;

                i++;
            }

            return -1;
        }
    }
}
