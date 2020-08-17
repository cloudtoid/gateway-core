using Microsoft.Net.Http.Headers;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class CookieSettings
    {
        internal CookieSettings(
            string name,
            bool? secure,
            bool? httpOnly,
            SameSiteMode sameSite,
            string? domain)
        {
            Name = name;
            Secure = secure;
            HttpOnly = httpOnly;
            SameSite = sameSite;
            Domain = domain;
        }

        public string Name { get; }

        public bool? Secure { get; }

        public bool? HttpOnly { get; }

        public SameSiteMode SameSite { get; }

        public string? Domain { get; }
    }
}