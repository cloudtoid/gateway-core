namespace Cloudtoid.GatewayCore.Settings
{
    public interface ISettingsProvider
    {
        ReverseProxySettings CurrentValue { get; }
    }
}
