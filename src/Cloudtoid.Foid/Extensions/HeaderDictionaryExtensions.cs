namespace Cloudtoid.Foid
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.AspNetCore.Http;

    [DebuggerStepThrough]
    internal static class HeaderDictionaryExtensions
    {
        public static void AddOrAppendHeaderValues(this IHeaderDictionary headers, string name, IEnumerable<string> values)
        {
            if (!headers.TryGetValue(name, out var currentValues))
            {
                headers.Add(name, values.AsArray());
                return;
            }

            headers[name] = currentValues.Concat(values).ToArray();
        }
    }
}
