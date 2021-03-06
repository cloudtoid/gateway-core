﻿using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.GatewayCore.Downstream
{
    /// <summary>
    /// By implementing this interface, one can have full control over the outbound downstream response headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="ResponseHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseHeaderSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseHeaderValuesProvider"/>, MyResponseHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseHeaderSetter"/>, MyResponseHeaderSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public interface IResponseHeaderSetter
    {
        Task SetHeadersAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken);
    }
}
