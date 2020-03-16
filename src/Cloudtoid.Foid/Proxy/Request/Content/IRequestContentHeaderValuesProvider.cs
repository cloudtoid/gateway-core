namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have some control over the outbound upstream content headers. Please consider the following extensibility points:
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
    public interface IRequestContentHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given request header.
        /// This interface is only used for content headers. See <see cref="IRequestHeaderValuesProvider"/> for request headers.
        /// Return <c>false</c> if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(
            HttpContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues);
    }
}
