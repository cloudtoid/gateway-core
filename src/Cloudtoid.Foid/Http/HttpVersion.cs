namespace Cloudtoid.Foid
{
    using System;
    using System.Collections.Generic;

    internal static class HttpVersion
    {
        internal static readonly Version Version10 = System.Net.HttpVersion.Version10;
        internal static readonly Version Version11 = System.Net.HttpVersion.Version11;
        internal static readonly Version Version20 = System.Net.HttpVersion.Version20;
        internal static readonly Version Version30 = new Version(3, 0);
        internal static readonly Version Version40 = new Version(4, 0);
        internal static readonly Version Version50 = new Version(5, 0);

        private static readonly IReadOnlyDictionary<string, Version> HttpVersionMap = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase)
        {
            { "HTTP/1", Version10 },
            { "HTTP/1.0", Version10 },
            { "HTTP/1.1", Version11 },
            { "HTTP/2", Version20 },
            { "HTTP/2.0", Version20 },
            { "HTTP/3", Version30 },
            { "HTTP/3.0", Version30 },
            { "HTTP/4", Version40 },
            { "HTTP/4.0", Version40 },
            { "HTTP/5", Version50 },
            { "HTTP/5.0", Version50 },
        };

        internal static Version? ParseOrDefault(string? protocol)
        {
            if (string.IsNullOrEmpty(protocol))
                return null;

            if (HttpVersionMap.TryGetValue(protocol, out var version))
                return version;

            var slash = protocol.LastIndexOf('/');
            if (slash != -1)
                protocol = protocol.Substring(slash + 1);

            if (!double.TryParse(protocol, out var numericVersion))
                return null;

            return numericVersion switch
            {
                1.0 => Version10,
                1.1 => Version11,
                2.0 => Version20,
                3.0 => Version30,
                4.0 => Version40,
                5.0 => Version50,
                _ => null,
            };
        }
    }
}
