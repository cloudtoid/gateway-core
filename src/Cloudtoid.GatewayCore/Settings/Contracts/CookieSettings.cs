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
            string? domainExpression)
        {
            Name = name;
            Secure = secure;
            HttpOnly = httpOnly;
            SameSite = sameSite;
            DomainExpression = domainExpression;
        }

        public string Name { get; }

        public bool? Secure { get; }

        public bool? HttpOnly { get; }

        public SameSiteMode SameSite { get; }

        public string? DomainExpression { get; }

        public string? EvaluateDomain(ProxyContext context)
            => context.Evaluate(DomainExpression);
    }
}