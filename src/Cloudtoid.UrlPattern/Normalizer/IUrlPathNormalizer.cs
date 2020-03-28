namespace Cloudtoid.UrlPattern
{
    public interface IUrlPathNormalizer
    {
        /// <summary>
        /// Normalizes the path segment of a URL.
        /// </summary>
        /// <remarks>
        /// <see cref="UrlPathNormalizer"/> for more information.
        /// </remarks>
        string Normalize(string path);
    }
}
