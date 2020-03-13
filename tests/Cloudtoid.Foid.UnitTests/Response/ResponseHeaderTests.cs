namespace Cloudtoid.Foid.UnitTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using static FoidOptions.ProxyOptions;

    [TestClass]
    public class ResponseHeaderTests
    {
        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithUnderscore_HeaderRemovedAsync()
        {
            // Arrange
            var message = new HttpResponseMessage();
            message.Headers.Add("X-Good-Header", "some-value");
            message.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var response = await SetHeadersAsync(message);

            // Assert
            response.Headers.ContainsKey("X-Good-Header").Should().BeTrue();
            response.Headers.ContainsKey("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenAllowHeadersWithUnderscore_HeaderKeptAsync()
        {
            // Arrange
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.AllowHeadersWithUnderscoreInName = true;

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Good-Header", "some-value");
            message.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers.ContainsKey("X-Good-Header").Should().BeTrue();
            response.Headers.ContainsKey("X_Bad_Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithEmptyValue_HeaderRemovedAsync()
        {
            // Arrange
            var message = new HttpResponseMessage();
            message.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var response = await SetHeadersAsync(message);

            // Assert
            response.Headers.ContainsKey("X-Empty-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenAllowHeaderWithEmptyValue_HeaderIsKeptAsync()
        {
            // Arrange
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.AllowHeadersWithEmptyValue = true;

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers.ContainsKey("X-Empty-Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenContentHeaderValue_HeaderIsNotIncludedAsync()
        {
            // Arrange
            var header = HeaderNames.ContentLocation;

            var message = new HttpResponseMessage
            {
                Content = new StringContent("test")
            };
            message.Content.Headers.Add(header, "some-value");

            // Act
            var response = await SetHeadersAsync(message);

            // Assert
            response.Headers.ContainsKey(header).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenCustomHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            // Arrange
            var provider = Substitute.For<IResponseHeaderValuesProvider>();
            provider
                .TryGetHeaderValues(
                    Arg.Any<HttpContext>(),
                    Arg.Is("X-Keep-Header"),
                    Arg.Any<string[]>(),
                    out Arg.Any<string[]>())
                .Returns(x =>
                {
                    x[3] = new[] { "keep-value" };
                    return true;
                });

            provider
                .TryGetHeaderValues(
                    Arg.Any<HttpContext>(),
                    Arg.Is("X-Drop-Header"),
                    Arg.Any<string[]>(),
                    out Arg.Any<string[]>())
                .Returns(false);

            var services = new ServiceCollection()
                .AddSingleton(provider)
                .AddTest();

            var serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IResponseHeaderSetter>();

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Keep-Header", "keep-value");
            message.Headers.Add("X-Drop-Header", "drop-value");

            // Act
            var context = new DefaultHttpContext();
            await setter.SetHeadersAsync(context, message);
            var response = context.Response;

            // Assert
            response.Headers.ContainsKey("X-Keep-Header").Should().BeTrue();
            response.Headers["X-Keep-Header"].Should().BeEquivalentTo(new[] { "keep-value" });
            response.Headers.ContainsKey("X-Drop-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreAllUpstreamResponseHeaders_NoDownstreamHeaderIsIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IgnoreAllUpstreamHeaders = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "abc");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers.ContainsKey(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreAllUpstreamResponseHeaders_DownstreamHeadersAreIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IgnoreAllUpstreamHeaders = false;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, new[] { "abc", "efg" });

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(new[] { "abc", "efg" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIncludeCorrelationId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IncludeCorrelationId = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIncludeCorrelationId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IncludeCorrelationId = false;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIncludeCorrelationIdWithNonDefaultHeaderName_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-test-id";
            var options = new FoidOptions();
            options.Proxy.CorrelationIdHeader = HeaderName;
            options.Proxy.Downstream.Response.Headers.IncludeCorrelationId = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIncludeCallId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IncludeCallId = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIncludeCallId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.IncludeCallId = false;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenExtraHeaders_HeadersIncludedAsync()
        {
            // Arrange
            var options = new FoidOptions();
            options.Proxy.Downstream.Response.Headers.Headers = new[]
            {
                new ExtraHeader
                {
                    Name = "x-xtra-1",
                    Values = new[] { "value1_1", "value1_2" }
                },
                new ExtraHeader
                {
                    Name = "x-xtra-2",
                    Values = new[] { "value2_1", "value2_2" }
                },
                new ExtraHeader
                {
                    Name = "x-xtra-3",
                    Values = new string[0]
                },
                new ExtraHeader
                {
                    Name = "x-xtra-4",
                    Values = null
                },
                new ExtraHeader
                {
                    Name = null,
                    Values = null
                },
            };

            var message = new HttpResponseMessage();
            message.Headers.Add("x-xtra-2", "value2_0");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers["x-xtra-1"].Should().BeEquivalentTo(new[] { "value1_1", "value1_2" });
            response.Headers["x-xtra-2"].Should().BeEquivalentTo(new[] { "value2_1", "value2_2" });
            response.Headers.ContainsKey("x-xtra-3").Should().BeFalse();
            response.Headers.ContainsKey("x-xtra-4").Should().BeFalse();
        }

        private static async Task<HttpResponse> SetHeadersAsync(
            HttpResponseMessage message,
            FoidOptions? options = null)
        {
            var services = new ServiceCollection().AddTest(options);
            var serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IResponseHeaderSetter>();
            var context = new DefaultHttpContext();
            await setter.SetHeadersAsync(context, message);
            return context.Response;
        }
    }
}