namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By inheriting from this class, one can have some control over the outbound upstream content headers. Please consider the following extensibility points:
    /// 1. Inherit from <see cref="RequestContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IRequestContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IRequestContentSetter"/> and register it with DI
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IRequestHeaderValuesProvider, MyRequestHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IRequestHeaderSetter, MyRequestHeaderSetter>()</c>
    /// </summary>
    public class RequestContentHeaderValuesProvider : IRequestContentHeaderValuesProvider
    {
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
