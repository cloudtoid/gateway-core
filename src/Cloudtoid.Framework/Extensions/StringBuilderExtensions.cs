namespace Cloudtoid
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    using static Contract;

    public static class StringBuilderExtensions
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendSpace(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(' ');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendQuote(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('"');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendColon(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(':');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendEqual(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('=');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendDollar(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('$');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendOpenParentheses(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('(');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendCloseParentheses(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(')');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendOpenBracket(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('[');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendCloseBracket(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(']');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendUnderscore(this StringBuilder builder) => builder.Append('_');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendGreaterThan(this StringBuilder builder) => builder.Append('>');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendLessThan(this StringBuilder builder) => builder.Append('<');

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendInParentheses(this StringBuilder builder, string value)
            => CheckValue(builder, nameof(builder)).AppendOpenParentheses().Append(value).AppendCloseParentheses();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendInBrackets(this StringBuilder builder, string value)
            => CheckValue(builder, nameof(builder)).AppendOpenBracket().Append(value).AppendCloseBracket();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendInQuotes(this StringBuilder builder, string value)
            => CheckValue(builder, nameof(builder)).AppendQuote().Append(value).AppendQuote();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendFormatInvariant(this StringBuilder builder, string format, params object[] args)
            => CheckValue(builder, nameof(builder)).AppendFormat(CultureInfo.InvariantCulture, format, args);

        [DebuggerStepThrough]
        public static StringBuilder Append<TItem>(this StringBuilder builder, IEnumerable<TItem> items, string? separator = null)
        {
            CheckValue(builder, nameof(builder));
            CheckValue(items, nameof(items));

            if (separator is null)
            {
                foreach (var item in items)
                    builder.Append(item);

                return builder;
            }

            var appendSeparator = false;
            foreach (var item in items)
            {
                if (appendSeparator)
                    builder.Append(separator);
                else
                    appendSeparator = true;

                builder.Append(item);
            }

            return builder;
        }

        [DebuggerStepThrough]
        public static StringBuilder Append<TItem>(this StringBuilder builder, IEnumerable<TItem> items, char separator)
        {
            CheckValue(builder, nameof(builder));
            CheckValue(items, nameof(items));

            var appendSeparator = false;
            foreach (var item in items)
            {
                if (appendSeparator)
                    builder.Append(separator);
                else
                    appendSeparator = true;

                builder.Append(item);
            }

            return builder;
        }
    }
}
