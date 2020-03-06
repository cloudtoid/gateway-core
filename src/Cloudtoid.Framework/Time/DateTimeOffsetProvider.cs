namespace Cloudtoid
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage("BannedApiAnalyzer", "RS0030", Justification = "Instead of the banned APIs, we should use the following extension methods.")]
    internal sealed class DateTimeOffsetProvider : IDateTimeOffsetProvider
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
