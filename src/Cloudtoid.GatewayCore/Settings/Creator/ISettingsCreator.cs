namespace Cloudtoid.GatewayCore.Settings
{
    internal interface ISettingsCreator
    {
        ReverseProxySettings Create(ReverseProxyOptions options);
    }
}
