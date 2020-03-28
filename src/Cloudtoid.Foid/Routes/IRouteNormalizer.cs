namespace Cloudtoid.Foid.Routes
{
    internal interface IRouteNormalizer
    {
        /// <summary>
        /// Normalizes the incoming downstream route.
        /// </summary>
        /// <remarks>
        /// <see cref="RouteNormalizer"/> for more information.
        /// </remarks>
        string Normalize(string route);
    }
}
