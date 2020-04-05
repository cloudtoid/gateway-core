namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// By implementing this interface, one can have full control over the outbound upstream request headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="RequestHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="RequestHeaderSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestHeaderSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestHeaderValuesProvider"/>, MyRequestHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestHeaderSetter"/>, MyRequestHeaderSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public interface IRequestHeaderSetter
    {
        Task SetHeadersAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken);
    }
}
