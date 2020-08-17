using Microsoft.Extensions.Primitives;

namespace Cloudtoid.GatewayCore.Downstream
{
    /// <summary>
    /// By implementing this interface, one can partially control the outbound downstream response content headers and trailing headers. Please, consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseContentHeaderValuesProvider"/>, MyResponseContentHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseContentSetter"/>, MyResponseContentSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public interface IResponseContentHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given response header.
        /// This interface is only used for content headers. See <see cref="IResponseHeaderValuesProvider"/> for response headers.
        /// Return <c>false</c> if the header must be omitted.
        /// </summary>
        bool TryGetHeaderValues(
            ProxyContext context,
            string name,
            StringValues downstreamValues,
            out StringValues upstreamValues);
    }
}
