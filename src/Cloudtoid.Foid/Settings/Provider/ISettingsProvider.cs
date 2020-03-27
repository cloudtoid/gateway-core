namespace Cloudtoid.Foid.Settings
{
    public interface ISettingsProvider
    {
        ReverseProxySettings CurrentValue { get; }
    }
}
