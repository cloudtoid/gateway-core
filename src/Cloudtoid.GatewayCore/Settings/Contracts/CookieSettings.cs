namespace Cloudtoid.GatewayCore.Settings
{
    public enum CookieAttributeBehavior
    {
        None,
        Add,
        Remove
    }

    public enum CookieSameSiteAttributeBehavior
    {
        None,
        Lax,
        Strict
    }

    public sealed class CookieSettings
    {
        internal CookieSettings(
            string name,
            CookieAttributeBehavior secure,
            CookieAttributeBehavior httpOnly,
            CookieSameSiteAttributeBehavior? sameSite,
            string? domain)
        {
            Name = name;
            Secure = secure;
            HttpOnly = httpOnly;
            SameSite = sameSite;
            Domain = domain;
        }

        public string Name { get; }

        public CookieAttributeBehavior Secure { get; }

        public CookieAttributeBehavior HttpOnly { get; }

        public CookieSameSiteAttributeBehavior? SameSite { get; }

        public string? Domain { get; }
    }
}