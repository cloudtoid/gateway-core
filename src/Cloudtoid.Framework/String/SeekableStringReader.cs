namespace Cloudtoid
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using static Contract;

    /// <summary>
    /// This class implements a seekable text reader that reads from a string.
    /// </summary>
    public class SeekableStringReader : TextReader
    {
        private string? value;
        private int length;
        private int pos;

        public SeekableStringReader(string value)
        {
            this.value = CheckValue(value, nameof(value));
            length = value.Length;
        }

        public virtual int Position
        {
            get
            {
                if (value is null)
                    throw new ObjectDisposedException(nameof(SeekableStringReader));

                return pos;
            }
            set => pos = CheckRange(value, 0, length - 1, nameof(Position));
        }

        public override void Close()
            => Dispose(true);

        protected override void Dispose(bool disposing)
        {
            value = null;
            pos = 0;
            length = 0;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Returns the next available character without actually reading it from
        /// the underlying string. The current position of the StringReader is not
        /// changed by this operation. The returned value is -1 if no further
        /// characters are available.
        /// </summary>
        public override int Peek()
        {
            if (value is null)
                throw new ObjectDisposedException(nameof(SeekableStringReader));

            if (pos == length)
                return -1;

            return value[pos];
        }

        /// <summary>
        /// Reads the next character from the underlying string. The returned value
        /// is -1 if no further characters are available.
        /// </summary>
        public override int Read()
        {
            if (value is null)
                throw new ObjectDisposedException(nameof(SeekableStringReader));

            if (pos == length)
                return -1;

            return value[pos++];
        }

        /// <summary>
        /// Reads a block of characters. This method will read up to count
        /// characters from this StringReader into the buffer character
        /// array starting at position index. Returns the actual number of
        /// characters read, or zero if the end of the string is reached.
        /// </summary>
        public override int Read(char[] buffer, int index, int count)
        {
            CheckValue(buffer, nameof(buffer));
            CheckNonNegative(index, nameof(index));
            CheckNonNegative(count, nameof(count));
            Check(buffer.Length - index >= count, "The length of the buffer is smaller than what is needed by index + count");
            if (value is null)
                throw new ObjectDisposedException(nameof(SeekableStringReader));

            int n = length - pos;
            if (n > 0)
            {
                if (n > count)
                    n = count;

                value.CopyTo(pos, buffer, index, n);
                pos += n;
            }

            return n;
        }

        public override int Read(Span<char> buffer)
        {
            if (value is null)
                throw new ObjectDisposedException(nameof(SeekableStringReader));

            int n = length - pos;
            if (n > 0)
            {
                if (n > buffer.Length)
                    n = buffer.Length;

                value.AsSpan(pos, n).CopyTo(buffer);
                pos += n;
            }

            return n;
        }

        public override int ReadBlock(Span<char> buffer)
            => Read(buffer);

        public override string ReadToEnd()
        {
            if (value is null)
                throw new ObjectDisposedException(nameof(SeekableStringReader));

            string s;
            if (pos == 0)
            {
                s = value;
            }
            else
            {
                s = value.Substring(pos, length - pos);
            }

            pos = length;
            return s;
        }

        /// <summary>
        /// Reads a line. A line is defined as a sequence of characters followed by
        /// a carriage return ('\r'), a line feed ('\n'), or a carriage return
        /// immediately followed by a line feed. The resulting string does not
        /// contain the terminating carriage return and/or line feed. The returned
        /// value is null if the end of the underlying string has been reached.
        /// </summary>
        public override string? ReadLine()
        {
            if (value is null)
                throw new ObjectDisposedException(nameof(SeekableStringReader));

            int i = pos;
            while (i < length)
            {
                char ch = value[i];
                if (ch == '\r' || ch == '\n')
                {
                    string result = value.Substring(pos, i - pos);
                    pos = i + 1;
                    if (ch == '\r' && pos < length && value[pos] == '\n')
                    {
                        pos++;
                    }

                    return result;
                }

                i++;
            }

            if (i > pos)
            {
                string result = value.Substring(pos, i - pos);
                pos = i;
                return result;
            }

            return null;
        }

        public override Task<string?> ReadLineAsync()
            => Task.FromResult(ReadLine());

        public override Task<string> ReadToEndAsync()
            => Task.FromResult(ReadToEnd());

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            CheckValue(buffer, nameof(buffer));
            CheckNonNegative(index, nameof(index));
            CheckNonNegative(count, nameof(count));
            Check(buffer.Length - index >= count, "The length of the buffer is smaller than what is needed by index + count");

            return Task.FromResult(ReadBlock(buffer, index, count));
        }

        public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested
                ? new ValueTask<int>(Task.FromCanceled<int>(cancellationToken))
                : new ValueTask<int>(ReadBlock(buffer.Span));
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
            => Task.FromResult(Read(buffer, index, count));

        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested
                ? new ValueTask<int>(Task.FromCanceled<int>(cancellationToken))
                : new ValueTask<int>(Read(buffer.Span));
        }
    }
}
