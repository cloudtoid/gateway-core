namespace Cloudtoid.Foid.Routes
{
    using System.Diagnostics.CodeAnalysis;
    using Cloudtoid.Foid.Options;

    internal interface IRouteSettingsCreator
    {
        bool TryCreate(
            string route,
            FoidOptions.RouteOptions options,
            [NotNullWhen(true)] out RouteSettings? result);
    }
}
