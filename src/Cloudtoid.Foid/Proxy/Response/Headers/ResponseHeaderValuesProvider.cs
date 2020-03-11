namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By inheriting from this clss, one can have some control over the outbound downstream response headers. Please consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IResponseHeaderSetter, MyResponseHeaderSetter>()</c>
    /// </summary>
    public class ResponseHeaderValuesProvider : IResponseHeaderValuesProvider
    {
        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            string[] upstreamHeaders,
            out string[] downstreamHeaders)
        {
            downstreamHeaders = upstreamHeaders;
            return true;
        }
    }
}
