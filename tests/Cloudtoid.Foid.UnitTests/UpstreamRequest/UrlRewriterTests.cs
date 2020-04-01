namespace Cloudtoid.Foid.UnitTests
{
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Upstream;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class UrlRewriterTests
    {
        [TestMethod]
        public async Task BasicUrlRewriteTestsAsync()
        {
            await RewriteAndValidateAsync(
                route: "/category",
                to: "https://cloudtoid.com/category/",
                url: "https://foid.com/category",
                expectedUrl: "https://cloudtoid.com/category/");
        }

        private static async Task RewriteAndValidateAsync(
            string route,
            string to,
            string url,
            string expectedUrl)
        {
            var options = TestExtensions.CreateDefaultOptions(route, to);
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            var serviceProvider = services.BuildServiceProvider();
            var urlRewriter = serviceProvider.GetRequiredService<IUrlRewriter>();

            UriHelper.FromAbsolute(
                url,
                out string scheme,
                out var host,
                out var path,
                out var query,
                out var _);

            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Scheme = scheme;
            request.Host = host;
            request.Path = path;
            request.QueryString = query;

            var context = serviceProvider.GetProxyContext(httpContext);
            var uri = await urlRewriter.RewriteUrlAsync(context, default);
            uri.ToString().Should().BeEquivalentTo(expectedUrl);
        }
    }
}
