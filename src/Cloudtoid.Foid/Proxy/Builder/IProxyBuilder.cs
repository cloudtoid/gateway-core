namespace Cloudtoid.Foid
{
    using Microsoft.Extensions.DependencyInjection;

    public interface IProxyBuilder
    {
        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Gets the HTTP client builder used by the outbound upstream HTTP request.
        /// </summary>
        IHttpClientBuilder RequestSenderHttpClientBuilder { get; }
    }
}
