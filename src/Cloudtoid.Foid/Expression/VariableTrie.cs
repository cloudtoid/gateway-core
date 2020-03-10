namespace Cloudtoid.Foid.Expression
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    internal sealed class VariableTrie<TValue> where TValue : class
    {
        // 37 == 26 * english chars + 1 * underscore
        private readonly VariableTrie<TValue>?[] map = new VariableTrie<TValue>?[27];
        private TValue? value;

        internal VariableTrie<TValue> AddValue(string key, TValue value)
        {
            var len = key.Length;
            var node = this;
            for (int i = 0; i < len; i++)
                node = node.GetNode(key[i]);

            node.value = value;
            return this;
        }

        internal bool TryGetBestMatch(
            string key,
            [NotNullWhen(true)] out TValue? value,
            out int lengthMatched)
        {
            (value, lengthMatched) = GetMatches(key).LastOrDefault();
            return value != null;
        }

        internal IEnumerable<(TValue Value, int LengthMatched)> GetMatches(string key)
        {
            var len = key.Length;
            int i = 0;
            var node = (VariableTrie<TValue>?)this;
            while (i < len)
            {
                node = node!.GetNodeOrDefault(key[i++]);
                if (node is null)
                    break;

                if (node.value != null)
                    yield return (node.value, i);
            }
        }

        private VariableTrie<TValue>? GetNodeOrDefault(char c)
            => !TryGetIndex(c, out var index) ? null : map[index];

        private VariableTrie<TValue> GetNode(char c)
        {
            if (!TryGetIndex(c, out var index))
            {
                throw new InvalidOperationException(
                    "The only valid characters in a variable name are alphabets and the underscore character.");
            }

            var node = map[index];
            if (node != null)
                return node;

            return map[index] = new VariableTrie<TValue>();
        }

        private static bool TryGetIndex(char c, out int index)
        {
            if (c == '_')
            {
                index = 26;
                return true;
            }

            int v = c;
            if (v > 96 && v < 123)
            {
                index = v - 97;
                return true;
            }

            if (v > 64 && v < 91)
            {
                index = v - 65;
                return true;
            }

            index = -1;
            return false;
        }
    }
}
