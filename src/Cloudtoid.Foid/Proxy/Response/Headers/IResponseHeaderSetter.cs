namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound downstream response headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton&lt;IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider&gt;()</c>
    /// 2. <c>TryAddSingleton&lt;IResponseHeaderSetter, MyResponseHeaderSetter&gt;()</c>
    /// </summary>
    public interface IResponseHeaderSetter
    {
        Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse);
    }
}
