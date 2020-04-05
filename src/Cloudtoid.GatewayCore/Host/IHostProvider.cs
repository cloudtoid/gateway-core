namespace Cloudtoid.GatewayCore.Host
{
    /// <summary>
    /// By implementing this interface, one can override the HOST header of the outbound upstream request.
    /// You can also inherit from <see cref="HostProvider"/> and register it with DI.
    /// </summary>
    public interface IHostProvider
    {
        /// <summary>
        /// Returns the value that must be used as the HOST header of the outbound upstream request.
        /// This method is only called once per request.
        /// </summary>
        string GetHost(ProxyContext context);
    }
}
