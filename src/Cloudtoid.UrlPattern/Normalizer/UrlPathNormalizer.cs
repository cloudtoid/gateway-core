namespace Cloudtoid.UrlPattern
{
    using System.Runtime.CompilerServices;

    internal sealed class UrlPathNormalizer : IUrlPathNormalizer
    {
        /// <summary>
        /// Normalizes the path segment of a URL by:
        /// <list type="bullet">
        /// <item>Trimming white spaces from the beginning and the end of the path. White spaces are defined by <see cref="char.IsWhiteSpace(char)"/>.</item>
        /// <item>Adds '/' to the beginning and the end of the path.</item>
        /// </list>
        /// </summary>
        public string Normalize(string path)
        {
            path = TrimWhiteSpaces(path);
            path = AppendSlashes(path);
            return path;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TrimWhiteSpaces(string path)
            => path.Trim();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string AppendSlashes(string path)
        {
            var len = path.Length;

            if (len == 0)
                return "/";

            if (path[len - 1] != '/')
                path += '/';

            if (len > 1 && path[0] != '/')
                path = '/' + path;

            return path;
        }
    }
}
