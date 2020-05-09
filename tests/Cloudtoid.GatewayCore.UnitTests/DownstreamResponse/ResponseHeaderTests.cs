namespace Cloudtoid.GatewayCore.UnitTests
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Downstream;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public sealed class ResponseHeaderTests
    {
        [TestMethod]
        public async Task SetHeadersAsync_HeaderWithUnderscore_HeaderRemovedAsync()
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
        public async Task SetHeadersAsync_AllowHeadersWithUnderscore_HeaderKeptAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.AllowHeadersWithUnderscoreInName = true;

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
        public async Task SetHeadersAsync_HeaderWithEmptyValue_HeaderRemovedAsync()
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
        public async Task SetHeadersAsync_ContentHeaderValue_HeaderIsNotIncludedAsync()
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
        public async Task SetHeadersAsync_CustomHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            // Arrange
            var provider = Substitute.For<IResponseHeaderValuesProvider>();
            var services = new ServiceCollection().AddSingleton(provider);

            provider
                .TryGetHeaderValues(
                    Arg.Any<ProxyContext>(),
                    Arg.Is("X-Keep-Header"),
                    Arg.Any<string[]>(),
                    out Arg.Any<string[]?>())
                .Returns(x =>
                {
                    x[3] = new[] { "keep-value" };
                    return true;
                });

            provider
                .TryGetHeaderValues(
                    Arg.Any<ProxyContext>(),
                    Arg.Is("X-Drop-Header"),
                    Arg.Any<string[]>(),
                    out Arg.Any<string[]?>())
                .Returns(false);

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Keep-Header", "keep-value");
            message.Headers.Add("X-Drop-Header", "drop-value");

            // Act
            var response = await SetHeadersAsync(message, services: services);

            // Assert
            response.Headers.ContainsKey("X-Keep-Header").Should().BeTrue();
            response.Headers["X-Keep-Header"].Should().BeEquivalentTo(new[] { "keep-value" });
            response.Headers.ContainsKey("X-Drop-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreAllUpstreamResponseHeaders_NoDownstreamHeaderIsIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.IgnoreAllUpstreamHeaders = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "some-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers.ContainsKey(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreAllUpstreamResponseHeaders_DownstreamHeadersAreIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.IgnoreAllUpstreamHeaders = false;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, new[] { "value-1", "value-2" });

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(new[] { "value-1", "value-2" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_IncludeCorrelationId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.IncludeCorrelationId = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIncludeCorrelationId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.IncludeCorrelationId = false;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetHeadersAsync_IncludeCorrelationIdWithNonDefaultHeaderName_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-test-id";
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.CorrelationIdHeader = HeaderName;
            proxy.DownstreamResponse.Headers.IncludeCorrelationId = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_IncludeCallId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.IncludeCallId = true;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEquivalentTo(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIncludeCallId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.IncludeCallId = false;

            var message = new HttpResponseMessage();
            message.Headers.Add(HeaderName, "old-value");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderName].Should().BeEmpty();
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameIsNull_DefaultViaHeaderAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = null;
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = false;
            proxy.DownstreamResponse.Headers.IgnoreVia = false;

            var message = new HttpResponseMessage();
            message.Version = new System.Version(2, 0);

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderNames.Via].Should().BeEquivalentTo(new[] { "2.0 gwcore" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNull_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = false;
            proxy.DownstreamResponse.Headers.IgnoreVia = false;

            var message = new HttpResponseMessage();

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderNames.Via].Should().BeEquivalentTo(new[] { "1.1 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeader_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = false;
            proxy.DownstreamResponse.Headers.IgnoreVia = false;

            var message = new HttpResponseMessage();
            message.Version = new System.Version(2, 0);
            message.Headers.Add(HeaderNames.Via, "1.0 test");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderNames.Via].Should().BeEquivalentTo(new[] { "1.0 test", "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeaders_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = false;
            proxy.DownstreamResponse.Headers.IgnoreVia = false;

            var message = new HttpResponseMessage();
            message.Version = new System.Version(2, 0);
            message.Headers.Add(HeaderNames.Via, "1.0 test, 1.1 test2");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderNames.Via].Should().BeEquivalentTo(new[] { "1.0 test", "1.1 test2", "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeadersAndIgnoreVia_ViaHeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = false;
            proxy.DownstreamResponse.Headers.IgnoreVia = true;

            var message = new HttpResponseMessage();
            message.Version = new System.Version(2, 0);
            message.Headers.Add(HeaderNames.Via, "1.0 test");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers.ContainsKey(HeaderNames.Via).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeadersAndIgnoreAllUpstreamHeaders_UpstreamViaHeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = true;
            proxy.DownstreamResponse.Headers.IgnoreVia = false;

            var message = new HttpResponseMessage();
            message.Version = new System.Version(2, 0);
            message.Headers.Add(HeaderNames.Via, "1.0 test, 1.1 test2");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderNames.Via].Should().BeEquivalentTo(new[] { "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaSeparateHeaders_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.DownstreamResponse.Headers.IgnoreAllUpstreamHeaders = false;
            proxy.DownstreamResponse.Headers.IgnoreVia = false;

            var message = new HttpResponseMessage();
            message.Version = new System.Version(2, 0);
            message.Headers.Add(HeaderNames.Via, new[] { "1.0 test", "1.1 test2" });

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers[HeaderNames.Via].Should().BeEquivalentTo(new[] { "1.0 test", "1.1 test2", "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ExtraHeaders_HeadersIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.DownstreamResponse.Headers;
            headersOptions.Overrides = new Dictionary<string, string[]>()
            {
                ["x-extra-1"] = new[] { "value1_1", "value1_2" },
                ["x-extra-2"] = new[] { "value2_1", "value2_2" },
                ["x-extra-3"] = new string[0],
                ["x-extra-4"] = null!,
            };

            var message = new HttpResponseMessage();
            message.Headers.Add("x-extra-0", "value0_0");
            message.Headers.Add("x-extra-2", "value2_0");
            message.Headers.Add("x-extra-3", "value3_0");
            message.Headers.Add("x-extra-4", "value4_0");

            // Act
            var response = await SetHeadersAsync(message, options);

            // Assert
            response.Headers["x-extra-0"].Should().BeEquivalentTo(new[] { "value0_0" });
            response.Headers["x-extra-1"].Should().BeEquivalentTo(new[] { "value1_1", "value1_2" });
            response.Headers["x-extra-2"].Should().BeEquivalentTo(new[] { "value2_1", "value2_2" });
            response.Headers.ContainsKey("x-extra-3").Should().BeFalse(); // should have been removed because its new value is empty
            response.Headers.ContainsKey("x-extra-4").Should().BeFalse(); // should have been removed because its new value is null
        }

        private static async Task<HttpResponse> SetHeadersAsync(
            HttpResponseMessage message,
            GatewayOptions? options = null,
            IServiceCollection? services = null)
        {
            services ??= new ServiceCollection();
            var serviceProvider = services.AddTest().AddTestOptions(options).BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IResponseHeaderSetter>();
            var context = serviceProvider.GetProxyContext();
            await setter.SetHeadersAsync(context, message, default);
            return context.Response;
        }
    }
}