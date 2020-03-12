namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound downstream response trailing headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// 1. Inherit from <see cref="TrailingHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="ITrailingHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="TrailingHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="ITrailingHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<ITrailingHeadersValuesProvider, MyTrailingHeadersValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<ITrailingHeadersSetter, MyTrailingHeadersSetter>()</c>
    /// </summary>
    public interface ITrailingHeaderSetter
    {
        Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse);
    }
}
