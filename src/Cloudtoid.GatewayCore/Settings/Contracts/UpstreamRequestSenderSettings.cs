namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class UpstreamRequestSenderSettings
    {
        internal UpstreamRequestSenderSettings(
            bool allowAutoRedirect,
            bool useCookies)
        {
            AllowAutoRedirect = allowAutoRedirect;
            UseCookies = useCookies;
        }

        public bool AllowAutoRedirect { get; }

        public bool UseCookies { get; }
    }
}
