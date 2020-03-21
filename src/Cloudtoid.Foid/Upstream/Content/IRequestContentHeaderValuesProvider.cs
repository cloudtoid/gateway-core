namespace Cloudtoid.Foid.Upstream
{
    using System.Collections.Generic;

    /// <summary>
    /// By implementing this interface, one can have some control over the outbound upstream content headers. Please consider the following extensibility points:
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
    public interface IRequestContentHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given request header.
        /// This interface is only used for content headers. See <see cref="IRequestHeaderValuesProvider"/> for request headers.
        /// Return <c>false</c> if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(
            ProxyContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues);
    }
}
