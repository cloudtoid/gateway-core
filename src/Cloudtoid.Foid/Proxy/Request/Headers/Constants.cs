namespace Cloudtoid.Foid.Proxy.Request
{
    internal static class Constants
    {
        internal static class Headers
        {
            internal const string ExternalAddress = "x-foid-external-address";
            internal const string ClientAddress = "x-forwarded-for";
            internal const string ClientProtocol = "x-forwarded-proto";
            internal const string RequestId = "x-request-id";
            internal const string CallId = "x-call-id";
            internal const string ProxyName = "x-foid-proxy-name";
        }
    }
}
