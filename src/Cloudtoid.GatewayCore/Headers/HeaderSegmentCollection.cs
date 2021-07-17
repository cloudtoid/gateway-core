using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Cloudtoid.GatewayCore.Headers
{
    internal readonly struct HeaderSegmentCollection : IEnumerable<string>
    {
        private readonly StringValues headers;

        public HeaderSegmentCollection(StringValues headers)
            => this.headers = headers;

        public Enumerator GetEnumerator()
            => new(headers);

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public struct Enumerator : IEnumerator<string>
        {
            private readonly StringValues headers;
            private int index;

            private string header;
            private int headerLength;
            private int offset;
            private int valueStart;
            private int valueEnd;

            private Mode mode;

            public Enumerator(StringValues headers)
            {
                this.headers = headers;
                header = string.Empty;
                headerLength = -1;
                index = -1;
                offset = -1;
                valueStart = -1;
                valueEnd = -1;
                mode = Mode.Leading;
            }

            private enum Mode
            {
                Leading,
                Value,
                ValueQuoted,
                Trailing,
                Produce,
            }

            private enum Attr
            {
                Value,
                Quote,
                Delimiter,
                Whitespace
            }

            public string Current
                => header[valueStart..valueEnd];

            object IEnumerator.Current
                => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                valueStart = -1;
                valueEnd = -1;

                if (!MoveNextHeaderIfNeeded())
                    return false;

                while (true)
                {
                    ++offset;
                    char ch = offset == headerLength ? (char)0 : header[offset];
                    Attr attr = char.IsWhiteSpace(ch)
                        ? Attr.Whitespace
                        : ch == '\"'
                            ? Attr.Quote
                            : (ch == ',' || ch == (char)0)
                                ? Attr.Delimiter
                                : Attr.Value;

                    switch (mode)
                    {
                        case Mode.Leading:
                            switch (attr)
                            {
                                case Attr.Delimiter:
                                    valueStart = valueStart == -1 ? offset : valueStart;
                                    valueEnd = valueEnd == -1 ? offset : valueEnd;
                                    mode = Mode.Produce;
                                    break;
                                case Attr.Quote:
                                    valueStart = offset;
                                    mode = Mode.ValueQuoted;
                                    break;
                                case Attr.Value:
                                    valueStart = offset;
                                    mode = Mode.Value;
                                    break;
                                case Attr.Whitespace:
                                default:
                                    break;
                            }

                            break;
                        case Mode.Value:
                            switch (attr)
                            {
                                case Attr.Quote:
                                    mode = Mode.ValueQuoted;
                                    break;
                                case Attr.Delimiter:
                                    valueEnd = offset;
                                    mode = Mode.Produce;
                                    break;
                                case Attr.Value:
                                    // more
                                    break;
                                case Attr.Whitespace:
                                    valueEnd = offset;
                                    mode = Mode.Trailing;
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case Mode.ValueQuoted:
                            switch (attr)
                            {
                                case Attr.Quote:
                                    mode = Mode.Value;
                                    break;
                                case Attr.Delimiter:
                                    if (ch == (char)0)
                                    {
                                        valueEnd = offset;
                                        mode = Mode.Produce;
                                    }

                                    break;
                                case Attr.Value:
                                case Attr.Whitespace:
                                default:
                                    break;
                            }

                            break;
                        case Mode.Trailing:
                            switch (attr)
                            {
                                case Attr.Delimiter:
                                    if (ch == (char)0)
                                        valueEnd = offset;

                                    mode = Mode.Produce;
                                    break;
                                case Attr.Quote:
                                    // back into value
                                    valueEnd = -1;
                                    mode = Mode.ValueQuoted;
                                    break;
                                case Attr.Value:
                                    // back into value
                                    valueEnd = -1;
                                    mode = Mode.Value;
                                    break;
                                case Attr.Whitespace:
                                default:
                                    break;
                            }

                            break;
                        case Mode.Produce:
                        default:
                            break;
                    }

                    if (mode == Mode.Produce)
                    {
                        if (valueEnd - valueStart > 1 && header[valueStart] == '"' && header[valueEnd - 1] == '"')
                        {
                            valueStart++;
                            valueEnd--;
                        }

                        mode = Mode.Leading;

                        if (valueEnd != valueStart)
                            return true;

                        if (!MoveNextHeaderIfNeeded())
                            return false;
                    }
                }
            }

            private bool MoveNextHeaderIfNeeded()
            {
                // if end of a string
                if (offset == headerLength)
                {
                    offset = -1;
                    valueStart = -1;
                    valueEnd = -1;

                    // if that was the last string
                    if (++index == headers.Count)
                        return false;

                    // grab the next string
                    header = headers[index] ?? string.Empty;
                    headerLength = header.Length;
                }

                return true;
            }

            public void Reset()
            {
                index = 0;
                offset = 0;
                valueStart = 0;
                valueEnd = 0;
            }
        }
    }
}
