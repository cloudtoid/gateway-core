namespace Cloudtoid.Foid
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    [DebuggerStepThrough]
    internal static class HeaderDictionaryExtensions
    {
        public static void AddOrAppendHeaderValues(this IHeaderDictionary headers, string key, params string[] values)
        {
            if (!headers.TryGetValue(key, out var currentValues))
            {
                headers.Add(key, values);
                return;
            }

            headers[key] = StringValues.Concat(currentValues, values);
        }
    }
}
