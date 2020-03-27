namespace Cloudtoid.Foid.Routes
{
    using System.Runtime.CompilerServices;

    internal sealed class RouteNormalizer : IRouteNormalizer
    {
        /// <summary>
        /// Normalizes the incoming downstream route by:
        /// <list type="bullet">
        /// <item>Trimming white spaces from the beginning and the end of the route. White spaces are defined by <see cref="char.IsWhiteSpace(char)"/>.</item>
        /// <item>Adds '/' to the beginning and the end of the route.</item>
        /// </list>
        /// </summary>
        public string Normalize(string route)
        {
            route = TrimWhiteSpaces(route);
            route = AppendSlashes(route);
            return route;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TrimWhiteSpaces(string route)
            => route.Trim();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string AppendSlashes(string route)
        {
            var len = route.Length;

            if (len == 0)
                return "/";

            if (route[len - 1] != '/')
                route += '/';

            if (len > 1 && route[0] != '/')
                route = '/' + route;

            return route;
        }
    }
}
