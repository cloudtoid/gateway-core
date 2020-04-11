namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class SystemSettings
    {
        internal SystemSettings(int routeCacheMaxCount)
        {
            RouteCacheMaxCount = routeCacheMaxCount;
        }

        public int RouteCacheMaxCount { get; }
    }
}
