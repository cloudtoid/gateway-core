namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound upstream content and its content headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// 1. Inherit from <see cref="RequestContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IRequestContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IRequestContentSetter"/> and register it with DI
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IRequestHeaderValuesProvider, MyRequestHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IRequestHeaderSetter, MyRequestHeaderSetter>()</c>
    /// </summary>
    public interface IRequestContentSetter
    {
        Task SetContentAsync(
            HttpContext context,
            HttpRequestMessage upstreamRequest);
    }
}
