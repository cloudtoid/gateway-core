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
                int c, len, start;

                Reset();

                void Reset()
                {
                    len = 0;
                    start = reader.NextPosition;
                }

                PatternNode? AppendMatch(string route)
                {
                    if (len > 0)
                        node += new MatchNode(route.Substring(start, len));

                    return node;
                }

                while ((c = reader.Read()) > -1)
                {
                    if (c == stopChar)
                        return AppendMatch(route);

                    PatternNode? next;
                    switch (c)
                    {
                        case PatternConstants.SegmentStart:
                            next = SegmentStartNode.Instance;
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
                            if (!ShouldEscape(reader))
                            {
                                len++;
                                continue;
                            }

                            AppendMatch(route);
                            start = reader.NextPosition += PatternConstants.EscapeSequence.Length;
                            len = 1;
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
                    Reset();
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
                int start = reader.NextPosition;
                int len = 0;

                int c = reader.Peek();
                if (c > -1 && char.IsDigit((char)c))
                {
                    error.AppendLine("The route pattern has a variable with an invalid name. Variables names cannot start with a number. The valid characters are 'a-zA-Z0-9_' and the first character cannot be a number.");
                    return null;
                }

                while (c > -1 && VariableNames.IsValidVariableChar(c))
                {
                    reader.Read();
                    c = reader.Peek();
                    len++;
                }

                if (len == 0)
                {
                    error.AppendLine("The route pattern has a variable with an empty or invalid name. The valid characters are 'a-zA-Z0-9_' and the first character cannot be a number.");
                    return null;
                }

                return new VariableNode(route.Substring(start, len));
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
            private static bool ShouldEscape(SeekableStringReader reader)
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
