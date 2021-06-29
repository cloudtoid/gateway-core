using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cloudtoid.UrlPattern;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Expression
{
    internal sealed class VariableTrie<TValue> where TValue : class
    {
        private readonly VariableTrie<TValue>?[] map = new VariableTrie<TValue>?[PatternVariables.ValidVariableCharacters.Length];
        private TValue? value;

        internal VariableTrie()
        {
        }

        internal VariableTrie(IEnumerable<(string Key, TValue Value)> list)
        {
            CheckValue(list, nameof(list));
            foreach (var (key, value) in list)
                AddValue(key, value);
        }

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
            return value is not null;
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

                if (node.value is not null)
                    yield return (node.value, i);
            }
        }

        private VariableTrie<TValue>? GetNodeOrDefault(char c)
            => !PatternVariables.TryGetIndex(c, out var index) ? null : map[index];

        private VariableTrie<TValue> GetNode(char c)
        {
            if (!PatternVariables.TryGetIndex(c, out var index))
            {
                throw new InvalidOperationException(
                    "The only valid characters in a variable name are alphabet, number, and underscore characters.");
            }

            var node = map[index];
            if (node is not null)
                return node;

            return map[index] = new VariableTrie<TValue>();
        }
    }
}
