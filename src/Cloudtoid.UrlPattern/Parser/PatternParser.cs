namespace Cloudtoid.UrlPattern
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using static Contract;

    internal sealed class PatternParser : IPatternParser
    {
        public bool TryParse(
            string pattern,
            [NotNullWhen(true)] out PatternNode? parsedPattern,
            [NotNullWhen(false)] out IReadOnlyList<PatternParserError>? errors)
        {
            CheckValue(pattern, nameof(pattern));

            pattern = pattern.Trim().Trim('/');
            (parsedPattern, errors) = new Parser(pattern).Parse();

            if (errors is null)
            {
                if (parsedPattern is null)
                    throw new InvalidOperationException($"Both {nameof(errors)} and {nameof(parsedPattern)} cannot be null");

                return true;
            }

            parsedPattern = null;
            return false;
        }

        private struct Parser
        {
            private readonly string route;
            private List<PatternParserError>? errors;

            public Parser(string route)
            {
                this.route = route;
                errors = null;
            }

            internal (PatternNode? Pattern, IReadOnlyList<PatternParserError>? Errors) Parse()
            {
                using (var reader = new SeekableStringReader(route))
                    return (ReadNode(reader), errors);
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
                        case Constants.SegmentStart:
                            next = SegmentStartNode.Instance;
                            break;

                        case Constants.Wildcard:
                            next = WildcardNode.Instance;
                            break;

                        case Constants.VariableStart:
                            next = ReadVariableNode(reader);
                            break;

                        case Constants.OptionalStart:
                            next = ReadOptionalNode(reader);
                            break;

                        case Constants.OptionalEnd:
                            AddError($"There is an unexpected '{(char)c}'.", reader.NextPosition - 1);
                            return null;

                        case Constants.EscapeSequenceStart:
                            if (!ShouldEscape(reader))
                            {
                                len++;
                                continue;
                            }

                            AppendMatch(route);
                            reader.NextPosition += Constants.EscapeSequence.Length;
                            start = reader.NextPosition - 1;
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
                    AddError($"There is a missing '{stopChar}'.");
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
                int c, len = 0;
                while ((c = reader.Peek()) > -1 && IsValidVariableChar(c, isFirstChar: len == 0))
                {
                    reader.Read();
                    len++;
                }

                if (len == 0)
                {
                    AddError(
                        "There is a variable with an empty or invalid name. The valid characters are 'a-zA-Z0-9_' and the first character cannot be a number.",
                        start);

                    return null;
                }

                return new VariableNode(route.Substring(start, len));
            }

            private OptionalNode? ReadOptionalNode(SeekableStringReader reader)
            {
                var start = reader.NextPosition;
                var node = ReadNode(reader, Constants.OptionalEnd);

                if (node == null)
                {
                    AddError("There is an optional element that is either empty or invalid.", start);
                    return null;
                }

                return new OptionalNode(node);
            }

            private void AddError(string message, int? location = null)
            {
                if (errors is null)
                    errors = new List<PatternParserError>();

                var error = new PatternParserError(message, location);
                errors.Add(error);
            }

            // returns true, if escaped an escapable char
            private static bool ShouldEscape(SeekableStringReader reader)
            {
                var value = reader.Value;
                var start = reader.NextPosition - 1;
                var len = Constants.EscapeSequence.Length;
                if (value.Length - reader.NextPosition < len)
                    return false;

                for (int i = 1; i < len; i++)
                {
                    if (Constants.EscapeSequence[i] != value[start + i])
                        return false;
                }

                return Constants.Escapable.Contains(value[start + len]);
            }

            /// <summary>
            /// The valid characters are [a..zA..Z0..9_]. However, the very first character cannot be a number.
            /// </summary>
            private static bool IsValidVariableChar(int c, bool isFirstChar)
            {
                if ((c > 64 && c < 91) || (c > 96 && c < 123) || c == 95)
                    return true;

                // numbers are only allowed if not the first character in the variable name
                if (!isFirstChar && c > 47 && c < 58)
                    return true;

                return false;
            }
        }
    }
}
