namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By inheriting from this class, one can have some control over the outbound upstream content headers. Please consider the following extensibility points:
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
    public class RequestContentHeaderValuesProvider : IRequestContentHeaderValuesProvider
    {
        /// <inheritdoc/>
        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues)
        {
            upstreamValues = downstreamValues;
            return true;
        }
    }
}
