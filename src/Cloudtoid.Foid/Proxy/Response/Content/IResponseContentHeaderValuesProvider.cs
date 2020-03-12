namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can partially control the outbound downstream response content headers and trailing headers. Please, consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseContentSetter"/> and register it with DI
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IResponseHeaderSetter, MyResponseHeaderSetter>()</c>
    /// </summary>
    public interface IResponseContentHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given response header.
        /// This interface is only used for content headers. See <see cref="IResponseHeaderValuesProvider"/> for response headers.
        /// Return <c>false</c> if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(
            HttpContext context,
            string name,
            string[] downstreamValues,
            out string[] upstreamValues);
    }
}
