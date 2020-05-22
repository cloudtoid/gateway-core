namespace Cloudtoid.GatewayCore.Headers
{
    internal static class Names
    {
        internal const string Forwarded = "Forwarded";
        internal const string XForwardedFor = "x-forwarded-for";
        internal const string XForwardedProto = "x-forwarded-proto";
        internal const string XForwardedHost = "x-forwarded-host";
        internal const string CorrelationId = "x-correlation-id";
        internal const string CallId = "x-call-id";
        internal static readonly string ExternalAddress = $"x-{Constants.ServerName}-external-address";
        internal static readonly string ProxyName = $"x-{Constants.ServerName}-proxy-name";
    }
}
