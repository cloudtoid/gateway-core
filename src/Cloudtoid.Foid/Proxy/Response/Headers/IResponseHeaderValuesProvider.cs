namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have some control over the outbound downstream response headers. Please consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IResponseHeaderSetter, MyResponseHeaderSetter>()</c>
    /// </summary>
    public interface IResponseHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given header.
        /// Return <c>false</c> if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(HttpContext context, string name, string[] upstreamValues, out string[] downstreamValues);
    }
}
