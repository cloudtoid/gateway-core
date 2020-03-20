namespace Cloudtoid.Foid
{
    using Cloudtoid.Foid.Options;

    public sealed class Route
    {
        internal Route(RouteOptions options)
        {
            Options = options;
        }

        public RouteOptions Options { get; }
    }
}
