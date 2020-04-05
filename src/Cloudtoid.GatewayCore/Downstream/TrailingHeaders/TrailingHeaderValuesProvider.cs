namespace Cloudtoid.GatewayCore.Downstream
{
    /// <summary>
    /// By inheriting from this class, one can have some control over the outbound downstream response trailing headers. Please consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="TrailingHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="ITrailingHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseHeaderSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="ITrailingHeaderValuesProvider"/>, MyTrailingHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseHeaderSetter"/>, MyResponseHeaderSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public class TrailingHeaderValuesProvider : ITrailingHeaderValuesProvider
    {
        public virtual bool TryGetHeaderValues(
            ProxyContext context,
            string name,
            string[] upstreamHeaders,
            out string[] downstreamHeaders)
        {
            downstreamHeaders = upstreamHeaders;
            return true;
        }
    }
}
