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

        /// <summary>
        /// These are the variables and their values extracted from the route pattern and the inbound URL path respectively.
        /// </summary>
        public IReadOnlyDictionary<string, string> Variables { get; }
    }
}
