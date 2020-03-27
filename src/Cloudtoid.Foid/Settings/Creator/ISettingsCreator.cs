namespace Cloudtoid.Foid.Settings
{
    internal interface ISettingsCreator
    {
        ReverseProxySettings Create(ReverseProxyOptions options);
    }
}
