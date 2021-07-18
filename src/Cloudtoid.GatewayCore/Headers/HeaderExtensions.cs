using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cloudtoid.GatewayCore.Headers
{
    internal static class HeaderExtensions
    {
        /// <summary>
        /// It comma separates the values of all headers with the <paramref name="name"/>,
        /// removing all leading and trailing white-space characters, removing empty values,
        /// and dequoting the values.
        /// </summary>
        public static HeaderSegmentCollection GetCommaSeparatedHeaderValues(this IHeaderDictionary headers, string name)
        {
            var values = GetHeaderUnmodified(headers, name);
            return new HeaderSegmentCollection(values);
        }

        public static StringValues GetHeaderUnmodified(this IHeaderDictionary headers, string key)
            => headers.TryGetValue(key, out var values) ? values : StringValues.Empty;
    }
}
