namespace Cloudtoid
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerStepThrough]
    public static class Set
    {
        public static ISet<T> Empty<T>() => EmptySet<T>.Instance;

        private sealed class EmptySet<T> : ISet<T>, IReadOnlyCollection<T>
        {
            private const string ReadOnlySetErrorMessage = "This is an empty and read-only set";
            private readonly ISet<T> set = new HashSet<T>();

            private EmptySet()
            {
            }

            public static ISet<T> Instance { get; } = new EmptySet<T>();

            public int Count => 0;

            public bool IsReadOnly => true;

            public bool Contains(T item) => false;

            public bool Add(T item) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            void ICollection<T>.Add(T item) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public void Clear() => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public bool Remove(T item) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException(ReadOnlySetErrorMessage);

            public void CopyTo(T[] array, int arrayIndex) => set.CopyTo(array, arrayIndex);

            public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);

            public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);

            public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);

            public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);

            public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);

            public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);

            public IEnumerator<T> GetEnumerator() => set.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => set.GetEnumerator();
        }
    }
}
