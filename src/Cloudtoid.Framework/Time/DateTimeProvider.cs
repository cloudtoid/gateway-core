namespace Cloudtoid
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage("BannedApiAnalyzer", "RS0030", Justification = "Instead of the banned APIs, we should use the following extension methods.")]
    internal sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
