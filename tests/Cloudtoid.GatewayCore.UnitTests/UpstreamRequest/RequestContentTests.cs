namespace Cloudtoid.GatewayCore.UnitTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Upstream;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class RequestContentTests
    {
        [TestMethod]
        public async Task SetContentAsync_WhenHasContentHeaders_ContentHeadersIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentDisposition;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out var headers).Should().BeTrue();
            headers.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task SetContentAsync_WhenHasCustomContentHeader_ContentHeaderIsIgnoredAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = "x-test-header";
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_WhenHasContentTypeHeader_ContentTypeHeadersIncludedOnlyOnceAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentType;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out var headers).Should().BeTrue();
            headers.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task SetContentAsync_WhenIgnoreHeaders_ContentHeadersNotIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentLocation;
            context.Request.Headers.Add(header, "some-value");

            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.UpstreamRequest.Headers.IgnoreAllDownstreamHeaders = true;

            // Act
            var message = await SetContentAsync(context, options: options);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_WhenNoContentHeader_NoContentHeaderAddedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.Accept;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        private static async Task<HttpRequestMessage> SetContentAsync(
            HttpContext httpContext,
            GatewayOptions? options = null)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            var serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IRequestContentSetter>();
            var context = serviceProvider.GetProxyContext(httpContext);
            context.Request.ContentLength = 10;
            var message = new HttpRequestMessage();
            await setter.SetContentAsync(context, message, default);
            return message;
        }
    }
}
