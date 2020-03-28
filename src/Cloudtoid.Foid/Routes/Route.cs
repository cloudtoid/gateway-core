namespace Cloudtoid.Foid
{
    using System.Collections.Generic;
    using Cloudtoid.Foid.Settings;

    public sealed class Route
    {
        internal Route(
            RouteSettings settings,
            IReadOnlyDictionary<string, string> variables)
        {
            Settings = settings;
            Variables = variables;
        }

        public RouteSettings Settings { get; }

        public IReadOnlyDictionary<string, string> Variables { get; }
    }
}
