namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Cloudtoid.Foid.Expression;
    using static Contract;

    internal sealed class PatternParser : IPatternParser
    {
        public bool TryParse(
            string route,
            out PatternNode? pattern,
            [MaybeNullWhen(true)] out string errors)
        {
            CheckValue(route, nameof(route));

            (pattern, errors) = new Parser(route).Parse();
            return errors is null;
        }

        private struct Parser
        {
            private readonly string route;
            private readonly StringBuilder error;

            public Parser(string route)
            {
                this.route = route;
                error = new StringBuilder();
            }

            internal (PatternNode? Pattern, string? Error) Parse()
            {
                using (var reader = new SeekableStringReader(route))
                    return (ReadNode(reader), error.Length == 0 ? null : error.ToString());
            }

            private PatternNode? ReadNode(SeekableStringReader reader, char? stopChar = null)
            {
                PatternNode? node = null;
                int c;
                int len = 0;
                int start = reader.NextPosition;
                while ((c = reader.Read()) > -1)
                {
                    if (c == stopChar)
                    {
                        if (len > 0)
                            node += new MatchNode(route.Substring(start, len));

                        return node;
                    }

                    PatternNode? next;
                    switch (c)
                    {
                        case PatternConstants.SegmentStart:
                            next = SegmentNode.Instance;
                            break;

                        case PatternConstants.Wildcard:
                            next = WildcardNode.Instance;
                            break;

                        case PatternConstants.VariableStart:
                            next = ReadVariableNode(reader);
                            break;

                        case PatternConstants.OptionalStart:
                            next = ReadOptionalNode(reader);
                            break;

                        case PatternConstants.OptionalEnd:
                            error.AppendLine($"There is an unexpected '{(char)c}'.");
                            return null;

                        case PatternConstants.EscapeSequenceStart:
                            if (ShouldEscape(reader))
                                reader.NextPosition += PatternConstants.EscapeSequence.Length;

                            len++;
                            continue;

                        default:
                            len++;
                            continue;
                    }

                    // failed to parse!
                    if (next is null)
                        return null;

                    if (len > 0)
                        node += new MatchNode(route.Substring(start, len));

                    node += next;
                    start = reader.NextPosition;
                    len = 0;
                }

                // expected an end char but didn't find it
                if (stopChar.HasValue)
                {
                    error.AppendLine($"There is a missing '{stopChar}'.");
                    return null;
                }

                node += len == 0
                    ? MatchNode.Empty
                    : new MatchNode(route.Substring(reader.NextPosition - len, len));

                return node;
            }

            private VariableNode? ReadVariableNode(SeekableStringReader reader)
            {
                int len = 0;
                int c;
                while ((c = reader.Peek()) > -1 && VariableNames.IsValidVariableChar(c))
                {
                    reader.Read();
                    len++;
                }

                if (len == 0)
                {
                    error.AppendLine("The route pattern has a variable with an empty or invalid name. The valid characters are 'a-zA-Z0-9'.");
                    return null;
                }

                return new VariableNode(route.Substring(reader.NextPosition - len, len));
            }

            private OptionalNode? ReadOptionalNode(SeekableStringReader reader)
            {
                var node = ReadNode(reader, PatternConstants.OptionalEnd);

                if (node == null)
                {
                    error.AppendLine("The route pattern has an optional element that seems to be empty or invalid.");
                    return null;
                }

                return new OptionalNode(node);
            }

            // returns true, if escaped an escapable char
            private bool ShouldEscape(SeekableStringReader reader)
            {
                var value = reader.Value;
                var start = reader.NextPosition - 1;
                var len = PatternConstants.EscapeSequence.Length;
                if (value.Length - reader.NextPosition < len)
                    return false;

                for (int i = 1; i < len; i++)
                {
                    if (PatternConstants.EscapeSequence[i] != value[start + i])
                        return false;
                }

                return PatternConstants.Escapable.Contains(value[start + len]);
            }
        }
    }
}
