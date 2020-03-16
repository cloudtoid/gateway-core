namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By inheriting from this class, one can partially control the outbound downstream response content headers. Please, consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseContentSetter"/> and register it with DI
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton&lt;IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider&gt;()</c>
    /// 2. <c>TryAddSingleton&lt;IResponseHeaderSetter, MyResponseHeaderSetter&gt;()</c>
    /// </summary>
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
