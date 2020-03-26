namespace Cloudtoid.Foid.Routes
{
    using System.Runtime.CompilerServices;

    internal sealed class RouteNormalizer : IRouteNormalizer
    {
        /// <summary>
        /// Normalizes the incoming downstream route  by:
        /// <list type="bullet">
        /// <item>Trimming '/' from the beginning and the end of the route.</item>
        /// <item>Trimming white spaces from the beginning and the end of the route. White spaces are defined by <see cref="char.IsWhiteSpace(char)"/>.</item>
        /// </list>
        /// </summary>
        public string Normalize(string route)
        {
            var len = route.Length;

            int left = 0;
            while (left < len)
            {
                if (!ShouldTrim(route[left]))
                    break;

                left++;
            }

            int right = len - 1;
            while (right > left)
            {
                if (!ShouldTrim(route[right]))
                    break;

                right--;
            }

            if (left == 0 && right == len - 1)
                return route;

            return route.Substring(left, right - left + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldTrim(char c)
            => c == '/' || char.IsWhiteSpace(c);
    }
}
