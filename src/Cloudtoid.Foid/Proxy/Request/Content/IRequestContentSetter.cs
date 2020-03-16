namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound upstream content and its content headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// <list type="bullet">
    /// <item><description>1. Inherit from <see cref="RequestContentHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>2. Implement <see cref="IRequestContentHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>3. Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>4. Implement <see cref="IRequestContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton&lt;IRequestHeaderValuesProvider, MyRequestHeaderValuesProvider&gt;()</c>
    /// 2. <c>TryAddSingleton&lt;IRequestHeaderSetter, MyRequestHeaderSetter&gt;()</c>
    /// </example>
    public interface IRequestContentSetter
    {
        /// <summary>
        /// Sets the content body and headers on <paramref name="upstreamRequest"/>.
        /// </summary>
        Task SetContentAsync(
            HttpContext context,
            HttpRequestMessage upstreamRequest);
    }
}
