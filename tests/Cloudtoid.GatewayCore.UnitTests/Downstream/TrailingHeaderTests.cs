using System.Net.Http;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Downstream;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed class TrailingHeaderTests
    {
        [TestMethod]
        public async Task SetHeadersAsync_AllowUnderscore_HeaderKeptAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.DiscardUnderscore = false;

            var message = new HttpResponseMessage();
            message.TrailingHeaders.Add("X-Good-Header", "some-value");
            message.TrailingHeaders.Add("X_Bad_Header", "some-value");

            // Act
            var response = await SetHeadersAsync(message);

            // Assert
            var headers = GetTrailingHeaders(response);
            headers.ContainsKey("X-Good-Header").Should().BeTrue();
            headers.ContainsKey("X_Bad_Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_DiscardUnderscore_HeaderDiscardedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.DiscardUnderscore = true;

            var message = new HttpResponseMessage();
            message.TrailingHeaders.Add("X-Good-Header", "some-value");
            message.TrailingHeaders.Add("X_Bad_Header", "some-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            var headers = GetTrailingHeaders(response);
            headers.ContainsKey("X-Good-Header").Should().BeTrue();
            headers.ContainsKey("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_HeaderWithEmptyValue_HeaderRemovedAsync()
        {
            // Arrange
            var message = new HttpResponseMessage();
            message.TrailingHeaders.Add("X-Empty-Header", string.Empty);

            // Act
            var response = await SetHeadersAsync(message);

            // Assert
            var headers = GetTrailingHeaders(response);
            headers.ContainsKey("X-Empty-Header").Should().BeFalse();
        }

        private static async Task<HttpResponse> SetHeadersAsync(
            HttpResponseMessage message,
            GatewayOptions? options = null,
            IServiceCollection? services = null)
        {
            services ??= new ServiceCollection();
            var serviceProvider = services.AddTest(gatewayOptions: options).BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<ITrailingHeaderSetter>();
            var context = serviceProvider.GetProxyContext();
            await setter.SetHeadersAsync(context, message, default);
            return context.Response;
        }

        private static IHeaderDictionary GetTrailingHeaders(HttpResponse response)
            => response.HttpContext.Features.Get<IHttpResponseTrailersFeature>().Trailers;
    }
}