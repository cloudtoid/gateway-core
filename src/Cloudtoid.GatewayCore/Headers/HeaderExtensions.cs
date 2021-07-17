using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cloudtoid.GatewayCore.Headers
{
    internal static class HeaderExtensions
    {
        public static HeaderSegmentCollection GetCommaSeparatedHeaderValues(this IHeaderDictionary headers, string key)
        {
            var values = GetHeaderUnmodified(headers, key);
            return new HeaderSegmentCollection(values);
        }

        public static StringValues GetHeaderUnmodified(this IHeaderDictionary headers, string key)
            => headers.TryGetValue(key, out var values) ? values : StringValues.Empty;
    }
}
