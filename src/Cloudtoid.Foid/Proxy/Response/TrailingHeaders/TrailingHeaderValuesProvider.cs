namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By inheriting from this class, one can have some control over the outbound downstream response trailing headers. Please consider the following extensibility points:
    /// 1. Inherit from <see cref="TrailingHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="ITrailingHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="TrailingHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="ITrailingHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton&lt;ITrailingHeadersValuesProvider, MyTrailingHeadersValuesProvider&gt;()</c>
    /// 2. <c>TryAddSingleton&lt;ITrailingHeadersSetter, MyTrailingHeadersSetter&gt;()</c>
    /// </summary>
    public class TrailingHeaderValuesProvider : ITrailingHeaderValuesProvider
    {
        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            string[] upstreamHeaders,
            out string[] downstreamHeaders)
        {
            downstreamHeaders = upstreamHeaders;
            return true;
        }
    }
}
