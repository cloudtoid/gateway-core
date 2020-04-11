namespace Cloudtoid.GatewayCore.UnitTests
{
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Downstream;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ResponseContentTests
    {
        [TestMethod]
        public async Task SetContentAsync_WhenHasContentBody_BodyIsCopiedAsync()
        {
            // Arrange
            const string value = "some-value";
            var message = new HttpResponseMessage();
            message.Content = new StringContent(value);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            var response = await SetContentAsync(message, context);

            // Assert
            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
            {
                (await reader.ReadToEndAsync()).Should().Be(value);
            }
        }

        [TestMethod]
        public async Task SetContentAsync_WhenIgnoreHeaders_ContentHeadersNotIncludedAsync()
        {
            // Arrange
            var header = HeaderNames.ContentDisposition;
            const string value = "some-value";
            var message = CreateHttpResponseMessage((header, value));

            var options = TestExtensions.CreateDefaultOptions();
            options.Routes.First().Value.Proxy!.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = true;

            // Act
            var response = await SetContentAsync(message, options: options);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_WhenHasContentHeaders_ContentHeadersIncludedAsync()
        {
            // Arrange
            var header = HeaderNames.ContentDisposition;
            const string value = "some-value";
            var message = CreateHttpResponseMessage((header, value));

            // Act
            var response = await SetContentAsync(message);

            // Assert
            response.Headers[header].Should().BeEquivalentTo(new[] { value });
        }

        [TestMethod]
        public async Task SetContentAsync_WhenHasTransferEncodingHeader_HeaderIsIgnoredAsync()
        {
            // Arrange
            var header = HeaderNames.TransferEncoding;
            var message = CreateHttpResponseMessage();
            const string value = "some-value";
            message.Content.Headers.TryAddWithoutValidation(header, value);

            // Act
            var response = await SetContentAsync(message);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_WhenNoContentHeader_NoContentHeaderAddedAsync()
        {
            // Arrange
            var header = HeaderNames.Accept;
            var message = CreateHttpResponseMessage();
            const string value = "some-value";
            message.Headers.Add(header, value);

            // Act
            var response = await SetContentAsync(message);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_WhenHasContentHeaders_ShouldOverrideExistingHeaderAsync()
        {
            // Arrange
            var header = HeaderNames.ContentDisposition;
            const string value = "good-value";

            var context = new DefaultHttpContext();
            context.Response.Headers.Add(header, "bad-value");

            var message = CreateHttpResponseMessage((header, value));

            // Act
            var response = await SetContentAsync(message, context);

            // Assert
            response.Headers[header].Should().BeEquivalentTo(new[] { value });
        }

        private static HttpResponseMessage CreateHttpResponseMessage(params (string Name, string Value)[] contentHeaders)
        {
            var message = new HttpResponseMessage
            {
                Content = new StringContent("test")
            };
            var headers = message.Content.Headers;
            foreach (var (name, value) in contentHeaders)
                headers.Add(name, value);

            return message;
        }

        private static async Task<HttpResponse> SetContentAsync(
            HttpResponseMessage message,
            HttpContext? httpContext = null,
            GatewayOptions? options = null)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            var serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IResponseContentSetter>();
            var context = serviceProvider.GetProxyContext(httpContext);

            await setter.SetContentAsync(context, message, default);
            return context.Response;
        }
    }
}
