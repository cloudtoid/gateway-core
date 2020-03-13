namespace Cloudtoid.Foid.UnitTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Options;
    using Cloudtoid.Foid.Proxy;
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
            var header = HeaderNames.ContentLength;
            context.Request.Headers.Add(header, "somevalue");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out var headers).Should().BeTrue();
            headers.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task SetContentAsync_WhenHasContentTypeHeader_ContentTypeHeadersIncludedOnlyOnceAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentType;
            context.Request.Headers.Add(header, "somevalue");

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
            var header = HeaderNames.ContentLength;
            context.Request.Headers.Add(header, "somevalue");

            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IgnoreAllUpstreamHeaders = true;

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
            context.Request.Headers.Add(header, "somevalue");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        private static async Task<HttpRequestMessage> SetContentAsync(
            HttpContext context,
            FoidOptions? options = null)
        {
            var services = new ServiceCollection().AddTest(options);
            var serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IRequestContentSetter>();
            var message = new HttpRequestMessage();
            await setter.SetContentAsync(context, message);
            return message;
        }
    }
}
