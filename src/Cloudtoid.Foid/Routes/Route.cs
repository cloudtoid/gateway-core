namespace Cloudtoid.Foid
{
    using Cloudtoid.Foid.Settings;

    public sealed class Route
    {
        internal Route(RouteSettings settings)
        {
            Settings = settings;
        }

        public RouteSettings Settings { get; }
    }
}
