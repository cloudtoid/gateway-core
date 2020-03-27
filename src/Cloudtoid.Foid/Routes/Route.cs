namespace Cloudtoid.Foid
{
    using Cloudtoid.Foid.Routes;

    public sealed class Route
    {
        internal Route(RouteSettings settings)
        {
            Settings = settings;
        }

        public RouteSettings Settings { get; }
    }
}
