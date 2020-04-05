namespace Cloudtoid.GatewayCore.UnitTests
{
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Upstream;
    using Cloudtoid.UrlPattern;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class UpstreamUrlCreatorTests
    {
        [TestMethod]
        public async Task BasicUrlCreationTestsAsync()
        {
            await CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https://upstream/category/",
                url: "https://gateway/category",
                expectedUrl: "https://upstream/category/");

            await CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https://upstream/category/",
                url: "https://gateway/category/c1/product/p1",
                expectedUrl: "https://upstream/category/c1/product/p1");

            await CreateAndValidateAsync(
                routePattern: "/category/:id",
                toExpression: "https://upstream/$id/category/",
                url: "https://gateway/category/c1",
                expectedUrl: "https://upstream/c1/category/");

            await CreateAndValidateAsync(
                routePattern: "/category/:id/product/:pid",
                toExpression: "https://upstream/$id/category/$pid",
                url: "https://gateway/category/c1/product/p1",
                expectedUrl: "https://upstream/c1/category/p1");

            await CreateAndValidateAsync(
                routePattern: "/cat*/:id/product/:pid",
                toExpression: "https://upstream/$id/category/$pid",
                url: "https://gateway/category/c1/product/p1",
                expectedUrl: "https://upstream/c1/category/p1");

            await CreateAndValidateAsync(
                routePattern: "/cat:id/product/:pid",
                toExpression: "https://upstream/$id/category/$pid",
                url: "https://gateway/catc1/product/p1",
                expectedUrl: "https://upstream/c1/category/p1");

            await CreateAndValidateAsync(
                routePattern: "/cat:id/product/:pid",
                toExpression: "https://upstream/$id/category/$pid",
                url: "https://gateway/catc1/product/p1/part/part1",
                expectedUrl: "https://upstream/c1/category/p1/part/part1");
        }

        [TestMethod]
        public void PathConcatTests()
        {
            UpstreamUrlCreator.ConcatPathWithSuffix(PathString.Empty, string.Empty).Value.Should().Be("/");
            UpstreamUrlCreator.ConcatPathWithSuffix(new PathString("/"), string.Empty).Value.Should().Be("/");
            UpstreamUrlCreator.ConcatPathWithSuffix(PathString.Empty, "/").Value.Should().Be("/");
            UpstreamUrlCreator.ConcatPathWithSuffix(new PathString("/"), "/").Value.Should().Be("/");

            UpstreamUrlCreator.ConcatPathWithSuffix("/left", string.Empty).Value.Should().Be("/left");
            UpstreamUrlCreator.ConcatPathWithSuffix(PathString.Empty, "/right").Value.Should().Be("/right");
            UpstreamUrlCreator.ConcatPathWithSuffix("/", "/right").Value.Should().Be("/right");
            UpstreamUrlCreator.ConcatPathWithSuffix("/left", "/").Value.Should().Be("/left/");
            UpstreamUrlCreator.ConcatPathWithSuffix("/left", "right").Value.Should().Be("/leftright");
            UpstreamUrlCreator.ConcatPathWithSuffix("/left", "/right").Value.Should().Be("/left/right");
            UpstreamUrlCreator.ConcatPathWithSuffix("/left/", "/right").Value.Should().Be("/left/right");
            UpstreamUrlCreator.ConcatPathWithSuffix("/left/", "right").Value.Should().Be("/left/right");
            UpstreamUrlCreator.ConcatPathWithSuffix("/left/", "right/").Value.Should().Be("/left/right/");
        }

        private static async Task CreateAndValidateAsync(
            string routePattern,
            string toExpression,
            string url,
            string expectedUrl)
        {
            var options = TestExtensions.CreateDefaultOptions(routePattern, toExpression);
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            var serviceProvider = services.BuildServiceProvider();
            var patternEngine = serviceProvider.GetRequiredService<IPatternEngine>();
            var urlRewriter = serviceProvider.GetRequiredService<IUpstreamUrlCreator>();

            UriHelper.FromAbsolute(
                url,
                out string scheme,
                out var host,
                out var path,
                out var query,
                out var fragment);

            patternEngine.TryMatch(
                routePattern,
                path.ToString() + query.ToString() + fragment.ToString(),
                out var match,
                out var why).Should().BeTrue();

            why.Should().BeNull();
            match.Should().NotBeNull();

            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Scheme = scheme;
            request.Host = host;
            request.Path = path;
            request.QueryString = query;

            var context = serviceProvider.GetProxyContext(httpContext, match!.PathSuffix, match.Variables);
            var uri = await urlRewriter.CreateAsync(context, default);
            uri.ToString().Should().BeEquivalentTo(expectedUrl);
        }
    }
}
