namespace Cloudtoid.GatewayCore.Settings
{
    internal interface ISettingsCreator
    {
        GatewaySettings Create(GatewayOptions options);
    }
}
