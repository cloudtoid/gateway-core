namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound upstream request headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// 1. Inherit from <see cref="RequestHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IRequestHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="RequestHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IRequestHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton&lt;IRequestHeaderValuesProvider, MyRequestHeaderValuesProvider&gt;()</c>
    /// 2. <c>TryAddSingleton&lt;IRequestHeaderSetter, MyRequestHeaderSetter&gt;()</c>
    /// </summary>
    public interface IRequestHeaderSetter
    {
        Task SetHeadersAsync(
            HttpContext context,
            HttpRequestMessage upstreamRequest);
    }
}
