namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound downstream response content and content headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseContentSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IResponseContentHeaderValuesProvider, MyResponseContentHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IResponseContentSetter, MyResponseContentSetter>()</c>
    /// </summary>
    public interface IResponseContentSetter
    {
        Task SetContentAsync(
            HttpContext context,
            HttpResponseMessage upstreamResponse);
    }
}
