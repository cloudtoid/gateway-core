namespace Cloudtoid.Foid.Settings
{
    using System.Diagnostics.CodeAnalysis;

    internal interface IRouteSettingsCreator
    {
        bool TryCreate(
            string route,
            ReverseProxyOptions.RouteOptions options,
            [NotNullWhen(true)] out RouteSettings? result);
    }
}
