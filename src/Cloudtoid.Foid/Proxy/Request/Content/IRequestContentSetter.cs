namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound upstream content and its content headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="RequestContentHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestContentHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestContentHeaderValuesProvider"/>, MyRequestContentHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestContentSetter"/>, MyRequestContentSetter&gt;()</c></description></item>
    /// </list>
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
