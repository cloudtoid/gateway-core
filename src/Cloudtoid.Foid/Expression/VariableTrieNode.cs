namespace Cloudtoid.Foid.Expression
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    internal sealed class VariableTrieNode<TValue> where TValue : class
    {
        private readonly VariableTrieNode<TValue>?[] map;
        private TValue? value;

        internal VariableTrieNode()
        {
            // 37 == 26 * english chars + 1 * underscore
            map = new VariableTrieNode<TValue>?[27];
        }

        internal void AddValue(string key, TValue value)
        {
            var len = key.Length;
            var node = this;
            for (int i = 0; i < len; i++)
                node = node.GetNode(key[i]);

            node.value = value;
        }

        internal bool TryGetBestMatch(
            string key,
            [NotNullWhen(true)] out TValue? value,
            out int lengthMatched)
        {
            value = default;
            lengthMatched = 0;

            var len = key.Length;
            int i = 0;
            var node = (VariableTrieNode<TValue>?)this;
            while (i < len)
            {
                node = node!.GetNodeOrDefault(key[i]);
                if (node is null)
                    break;

                value = node.value;
                lengthMatched = ++i;
            }

            return value != default;
        }

        internal IEnumerable<(TValue Value, int LengthMatched)> GetValues(string key)
        {
            var len = key.Length;
            int i = 0;
            var node = (VariableTrieNode<TValue>?)this;
            while (i < len)
            {
                node = node!.GetNodeOrDefault(key[i++]);
                if (node is null)
                    break;

                if (node.value != null)
                    yield return (node.value, i);
            }
        }

        private VariableTrieNode<TValue>? GetNodeOrDefault(char c)
            => !TryGetIndex(c, out var index) ? null : map[index];

        private VariableTrieNode<TValue> GetNode(char c)
        {
            if (!TryGetIndex(c, out var index))
                throw new InvalidOperationException("The only valid characters in a variable name are alphabets and underscore.");

            var node = map[index];
            if (node != null)
                return node;

            return map[index] = new VariableTrieNode<TValue>();
        }

        private static bool TryGetIndex(char c, out int index)
        {
            if (c == '_')
            {
                index = 26;
                return true;
            }

            int a = c;
            if (a > 96 && a < 123)
            {
                index = a - 97;
                return true;
            }

            if (a > 64 && a < 91)
            {
                index = a - 65;
                return true;
            }

            index = -1;
            return false;
        }
    }
}
