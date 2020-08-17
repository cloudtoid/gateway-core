using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.GatewayCore.Upstream
{
    /// <summary>
    /// Creates the URL for the outbound upstream request.
    /// By implementing this interface, one can generate their desired URL for the upstream request.
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <c>TryAddSingleton&lt;<see cref="IUpstreamUrlCreator"/>, MyUpstreamUrlCreator&gt;()</c>
    /// </example>
    public interface IUpstreamUrlCreator
    {
        /// <summary>
        /// Creates the URL for the outbound upstream request.
        /// </summary>
        Task<Uri> CreateAsync(
            ProxyContext context,
            CancellationToken cancellationToken);
    }
}
