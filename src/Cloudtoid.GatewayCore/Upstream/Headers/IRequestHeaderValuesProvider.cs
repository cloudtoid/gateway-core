namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Collections.Generic;

    /// <summary>
    /// By implementing this interface, one can have some control over the outbound upstream request headers. Please consider the following extensibility points:
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
    public interface IRequestHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given request header.
        /// This interface is only used for request headers. See <see cref="IRequestContentHeaderValuesProvider"/> for content headers.
        /// Return <c>false</c> if the header must be omitted.
        /// </summary>
        bool TryGetHeaderValues(
            ProxyContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues);
    }
}
