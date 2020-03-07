namespace Cloudtoid
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using static Contract;

    [DebuggerStepThrough]
    public static class HttpUtil
    {
        private static readonly IReadOnlyDictionary<string, HttpMethod> HttpMethods = new Dictionary<string, HttpMethod>(StringComparer.OrdinalIgnoreCase)
        {
            { HttpMethod.Get.Method, HttpMethod.Get },
            { HttpMethod.Post.Method, HttpMethod.Post },
            { HttpMethod.Options.Method, HttpMethod.Options },
            { HttpMethod.Head.Method, HttpMethod.Head },
            { HttpMethod.Delete.Method, HttpMethod.Delete },
            { HttpMethod.Patch.Method, HttpMethod.Patch },
            { HttpMethod.Put.Method, HttpMethod.Put },
            { HttpMethod.Trace.Method, HttpMethod.Trace },
        };

        public static HttpMethod GetHttpMethod(string method)
        {
            CheckNonEmpty(method, nameof(method));
            return HttpMethods.TryGetValue(method, out var m) ? m : new HttpMethod(method);
        }
    }
}
