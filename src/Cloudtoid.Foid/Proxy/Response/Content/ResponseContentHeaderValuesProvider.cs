namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By inheriting from this class, one can partially control the outbound downstream response content headers. Please, consider the following extensibility points:
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
    public class ResponseContentHeaderValuesProvider : IResponseContentHeaderValuesProvider
    {
        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            string[] downstreamValues,
            out string[] upstreamValues)
        {
            upstreamValues = downstreamValues;
            return true;
        }
    }
}
