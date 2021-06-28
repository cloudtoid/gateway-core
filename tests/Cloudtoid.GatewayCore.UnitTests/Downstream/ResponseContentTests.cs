using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Downstream;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed class ResponseContentTests
    {
        private IServiceProvider? serviceProvider;

        [TestMethod]
        public async Task SetContentAsync_HasContentBody_BodyIsCopiedAsync()
        {
            // Arrange
            const string value = "some-value";
            var message = new HttpResponseMessage
            {
                Content = new StringContent(value),
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            var response = await SetContentAsync(message, context);

            // Assert
            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
                (await reader.ReadToEndAsync()).Should().Be(value);
        }

        [TestMethod]
        public async Task SetContentAsync_NullContent_BodyIsCopiedAsync()
        {
            // Arrange
            var message = new HttpResponseMessage
            {
                Content = null
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            var response = await SetContentAsync(message, context);

            // Assert
            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
                (await reader.ReadToEndAsync()).Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetContentAsync_ZeroContentLength_BodyIsCopiedAsync()
        {
            // Arrange
            var message = new HttpResponseMessage
            {
                Content = new StringContent(string.Empty),
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            var response = await SetContentAsync(message, context);

            // Assert
            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
                (await reader.ReadToEndAsync()).Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetContentAsync_HeaderRejectedByIResponseContentHeaderValuesProvider_LogsInfoAsync()
        {
            // Arrange
            var header = HeaderNames.ContentMD5;
            const string value = "Q2hlY2sgSW50ZWdyaXR5IQ==";
            var message = CreateHttpResponseMessage((header, value));
            var provider = new DropContentHeaderValuesProvider();

            // Act
            var response = await SetContentAsync(message, provider: provider);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
            var logger = (Logger<ResponseContentSetter>)serviceProvider.GetRequiredService<ILogger<ResponseContentSetter>>();
            logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase("Header 'Content-MD5' is not added. This was instructed by IResponseContentHeaderValuesProvider.TryGetHeaderValues.")).Should().BeTrue();
        }

        [TestMethod]
        public async Task SetContentAsync_IgnoreHeaders_ContentHeadersNotIncludedAsync()
        {
            // Arrange
            var header = HeaderNames.ContentDisposition;
            const string value = "some-value";
            var message = CreateHttpResponseMessage((header, value));

            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.DownstreamResponse.Headers.DiscardInboundHeaders = true;

            // Act
            var response = await SetContentAsync(message, options: options);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_HasContentHeaders_ContentHeadersIncludedAsync()
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
        public async Task SetContentAsync_HasTransferEncodingHeader_HeaderIsIgnoredAsync()
        {
            // Arrange
            var header = HeaderNames.TransferEncoding;
            var message = CreateHttpResponseMessage((header, "chunked"));

            // Act
            var response = await SetContentAsync(message);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_NoContentHeader_NoContentHeaderAddedAsync()
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
        public async Task SetContentAsync_HasEmptyContentHeader_NoContentHeaderAddedAsync()
        {
            // Arrange
            var header = HeaderNames.ContentLanguage;
            var message = CreateHttpResponseMessage((header, string.Empty));

            // Act
            var response = await SetContentAsync(message);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_HasContentHeaders_ShouldOverrideExistingHeaderAsync()
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
                headers.TryAddWithoutValidation(name, value);

            return message;
        }

        private async Task<HttpResponse> SetContentAsync(
            HttpResponseMessage message,
            HttpContext? httpContext = null,
            GatewayOptions? options = null,
            IResponseContentHeaderValuesProvider? provider = null)
        {
            var services = new ServiceCollection();

            if (provider != null)
                services.TryAddSingleton(provider);

            services.AddTest().AddTestOptions(options);
            serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IResponseContentSetter>();
            var context = serviceProvider.GetProxyContext(httpContext);

            await setter.SetContentAsync(context, message, default);
            return context.Response;
        }

        private sealed class DropContentHeaderValuesProvider : ResponseContentHeaderValuesProvider
        {
            public override bool TryGetHeaderValues(
                ProxyContext context,
                string name,
                StringValues downstreamValues,
                out StringValues upstreamValues)
            {
                if (name.EqualsOrdinalIgnoreCase(HeaderNames.ContentMD5))
                {
                    upstreamValues = StringValues.Empty;
                    return false;
                }

                return base.TryGetHeaderValues(context, name, downstreamValues, out upstreamValues);
            }
        }
    }
}
