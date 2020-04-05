namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Collections.Generic;

    /// <summary>
    /// By inheriting from this class, one can have some control over the outbound upstream content headers. Please consider the following extensibility points:
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
    public class RequestContentHeaderValuesProvider : IRequestContentHeaderValuesProvider
    {
        /// <inheritdoc/>
        public virtual bool TryGetHeaderValues(
            ProxyContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues)
        {
            upstreamValues = downstreamValues;
            return true;
        }
    }
}
