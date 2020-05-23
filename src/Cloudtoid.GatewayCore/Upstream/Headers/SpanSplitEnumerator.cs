// DELETE this once .NET Core has support for Span<T>.Split()
// This code was taken from an upcoming version of .NET
namespace Cloudtoid
{
    using System;

    internal ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> sequence;
        private readonly T separator;
        private int offset;
        private int index;

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
        {
            sequence = span;
            this.separator = separator;
            index = 0;
            offset = 0;
        }

        public readonly ReadOnlySpan<T> Current => sequence.Slice(offset, index - 1);

        public bool MoveNext()
        {
            if (sequence.Length - offset < index)
                return false;

            var slice = sequence.Slice(offset += index);

            var nextIdx = slice.IndexOf(separator);
            index = (nextIdx != -1 ? nextIdx : slice.Length) + 1;
            return true;
        }
    }

    internal static class MemoryExtensions
    {
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator)
            => new SpanSplitEnumerator<char>(span, separator);
    }
}