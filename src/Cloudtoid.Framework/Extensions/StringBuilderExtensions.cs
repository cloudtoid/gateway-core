namespace Cloudtoid
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    using static Contract;

    [DebuggerStepThrough]
    public static class StringBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendSpace(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(' ');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendQuote(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('"');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendColon(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(':');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendEqual(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('=');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendDollar(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('$');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendOpenParentheses(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('(');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendCloseParentheses(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(')');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendOpenBracket(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append('[');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendCloseBracket(this StringBuilder builder)
            => CheckValue(builder, nameof(builder)).Append(']');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendUnderscore(this StringBuilder builder) => builder.Append('_');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendGreaterThan(this StringBuilder builder) => builder.Append('>');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendLessThan(this StringBuilder builder) => builder.Append('<');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendInParentheses(this StringBuilder builder, string value)
            => CheckValue(builder, nameof(builder)).AppendOpenParentheses().Append(value).AppendCloseParentheses();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendInBrackets(this StringBuilder builder, string value)
            => CheckValue(builder, nameof(builder)).AppendOpenBracket().Append(value).AppendCloseBracket();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendInQuotes(this StringBuilder builder, string value)
            => CheckValue(builder, nameof(builder)).AppendQuote().Append(value).AppendQuote();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder AppendFormatInvariant(this StringBuilder builder, string format, params object[] args)
            => CheckValue(builder, nameof(builder)).AppendFormat(CultureInfo.InvariantCulture, format, args);

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
