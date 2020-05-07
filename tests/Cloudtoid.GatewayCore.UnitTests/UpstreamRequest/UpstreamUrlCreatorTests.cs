namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
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

            await CreateAndValidateAsync(
                routePattern: "/cat/:cid/product/:pid",
                toExpression: "https://upstream/$cid/$pid?q0=5#f1",
                url: "https://gateway/cat/c1/product/p1/part1/part2?q1=10&q2=20",
                expectedUrl: "https://upstream/c1/p1/part1/part2?q0=5&q1=10&q2=20#f1");

            await CreateAndValidateAsync(
                routePattern: "/cat/:cid/product/:pid",
                toExpression: "https://upstream/inventory?cid=$cid#$pid",
                url: "https://gateway/cat/c1/product/p1/part1/part2?top=10",
                expectedUrl: "https://upstream/inventory/part1/part2?cid=c1&top=10#p1");
        }

        [TestMethod]
        public void ToUrlWithMalformedScheme()
        {
            Func<Task> act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https:::://upstream/category/",
                url: "https://gateway/category/",
                expectedUrl: "https://upstream/");

            act.Should().ThrowExactly<UriFormatException>("The HTTP scheme 'https:::' specified by 'https:::://upstream/category/' expression is invalid.");
        }

        [TestMethod]
        public void ToUrlWithMalformedHost()
        {
            Func<Task> act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https:///category/",
                url: "https://gateway/category/",
                expectedUrl: "https://upstream/");

            act.Should().ThrowExactly<UriFormatException>("The URL host '' specified by 'https:///category/' expression is invalid.");

            act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https://192.168.0.1/category/",
                url: "https://gateway/category/",
                expectedUrl: "https://192.168.0.1/category/");

            act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https://[fe80::20c:29ff:fee2:1de]/category/",
                url: "https://gateway/category/",
                expectedUrl: "https://[fe80::20c:29ff:fee2:1de]/category/");

            act.Should().NotThrow();

            act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https://[fe80::20c:29ff:fee2:1de]:10/category/",
                url: "https://gateway/category/",
                expectedUrl: "https://[fe80::20c:29ff:fee2:1de]:10/category/");

            act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https://192.168.0.1:10/category/",
                url: "https://gateway/category/",
                expectedUrl: "https://192.168.0.1:10/category/");

            act.Should().NotThrow();
        }

        [TestMethod]
        public void ToUrlFullyMalformed()
        {
            Func<Task> act = () => CreateAndValidateAsync(
                routePattern: "/category",
                toExpression: "https: / /category/",
                url: "https://gateway/category/",
                expectedUrl: "https://upstream/");

            act.Should().ThrowExactly<UriFormatException>("Using 'https: / /category/' expression to rewrite request URL '/category/'. However, the rewritten URL is not a valid URL format.");
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
                out var _);

            patternEngine.TryMatch(
                routePattern,
                path.ToString(),
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
