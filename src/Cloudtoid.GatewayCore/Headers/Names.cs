namespace Cloudtoid.GatewayCore.Headers
{
    internal static class Names
    {
        internal const string ExternalAddress = "x-gwcore-external-address";
        internal const string ProxyName = "x-gwcore-proxy-name";
        internal const string ForwardedFor = "x-forwarded-for";
        internal const string ForwardedProtocol = "x-forwarded-proto";
        internal const string ForwardedHost = "x-forwarded-host";
        internal const string CorrelationId = "x-correlation-id";
        internal const string CallId = "x-call-id";
    }
}
