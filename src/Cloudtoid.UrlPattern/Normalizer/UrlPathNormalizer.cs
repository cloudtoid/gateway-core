namespace Cloudtoid.UrlPattern
{
    using System.Text;

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
            int len = path.Length;

            int startIndex = 0;
            while (startIndex < len)
            {
                var c = path[startIndex];
                if (!char.IsWhiteSpace(c) && c != '/')
                    break;

                startIndex++;
            }

            int endIndex = len - 1;
            while (startIndex < endIndex)
            {
                var c = path[endIndex];
                if (!char.IsWhiteSpace(c) && c != '/')
                    break;

                endIndex--;
            }

            var sublen = endIndex - startIndex + 1;
            var builder = new StringBuilder(sublen + 2)
                .AppendSlash()
                .Append(path, startIndex, sublen);

            if (builder[builder.Length - 1] != '/')
                builder.AppendSlash();

            return builder.ToString();
        }
    }
}
