namespace Cloudtoid.GatewayCore.Settings
{
    public interface ISettingsProvider
    {
        GatewaySettings CurrentValue { get; }
    }
}
