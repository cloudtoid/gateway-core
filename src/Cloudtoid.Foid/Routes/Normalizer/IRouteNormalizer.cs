namespace Cloudtoid.Foid.Routes
{
    internal interface IRouteNormalizer
    {
        /// <summary>
        /// Normalizes the incoming downstream route  by:
        /// <list type="bullet">
        /// <item>Adds '/' to the beginning and the end of the route.</item>
        /// <item>Trimming white spaces from the beginning and the end of the route. White spaces are defined by <see cref="char.IsWhiteSpace(char)"/>.</item>
        /// </list>
        /// </summary>
        string Normalize(string route);
    }
}
