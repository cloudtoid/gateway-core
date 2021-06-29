using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Upstream;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed partial class RequestHeaderTests
    {
        [TestMethod]
        public async Task SetHeadersAsync_HostHeaderIncluded_HostHeaderIsNotAddedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, "my-host");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_AllowUnderscore_HeaderKeptAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardUnderscore = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains("X-Good-Header").Should().BeTrue();
            message.Headers.Contains("X_Bad_Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_DiscardWithUnderscore_HeaderDiscardedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardUnderscore = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains("X-Good-Header").Should().BeTrue();
            message.Headers.Contains("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_AllowEmptyValue_HeaderKeptAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardEmpty = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains("X-Empty-Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_DiscardEmptyValue_HeaderDiscardedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardEmpty = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains("X-Empty-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_HasContentHeader_ContentHeaderNotIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentType;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_HasStandardHopByHopeHeader_HopByHopeHeaderNotIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.KeepAlive;
            context.Request.Headers.Add(header, "timeout=5");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_CustomHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            // Arrange
            var provider = Substitute.For<IRequestHeaderValuesProvider>();
            var services = new ServiceCollection().AddSingleton(provider);

            provider
                .TryGetHeaderValues(
                    Arg.Any<ProxyContext>(),
                    Arg.Is("X-Keep-Header"),
                    Arg.Any<StringValues>(),
                    out Arg.Any<StringValues>())
                .Returns(x =>
                {
                    x[3] = new StringValues("keep-value");
                    return true;
                });

            provider
                .TryGetHeaderValues(
                    Arg.Any<ProxyContext>(),
                    Arg.Is("X-Drop-Header"),
                    Arg.Any<StringValues>(),
                    out Arg.Any<StringValues>())
                .Returns(false);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("X-Keep-Header", "keep-value");
            httpContext.Request.Headers.Add("X-Drop-Header", "drop-value");

            // Act
            var message = await SetHeadersAsync(httpContext, services: services);

            // Assert
            message.Headers.Contains("X-Keep-Header").Should().BeTrue();
            message.Headers.GetValues("X-Keep-Header").Should().BeEquivalentTo(new[] { "keep-value" });
            message.Headers.Contains("X-Drop-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_AddExternalAddress_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-external-address";

            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.AddExternalAddress = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "3.2.1.0");
            context.Connection.RemoteIpAddress = IpV4Sample;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be(IpV4Sample.ToString());
        }

        [TestMethod]
        public async Task SetHeadersAsync_AddExternalAddressButNullRemoteAddress_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-external-address";

            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.AddExternalAddress = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "3.2.1.0");
            context.Connection.RemoteIpAddress = null;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotAddExternalAddress_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-external-address";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.AddExternalAddress = false;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV4Sample;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreAllDownstreamHeaders_NoDownstreamHeaderIsIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardInboundHeaders = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreAllDownstreamHeaders_DownstreamHeadersAreIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardInboundHeaders = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, new[] { "first-value", "second-value" });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).Should().BeEquivalentTo(new[] { "first-value", "second-value" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreAllDownstreamHeadersAndCorrelationId_NewCorrelationIdIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardInboundHeaders = true;
            headersOptions.SkipCorrelationId = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreAllDownstreamHeadersAndCorrelationId_ExistingCorrelationIdIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.DiscardInboundHeaders = false;
            headersOptions.SkipCorrelationId = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be("some-value");
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreCorrelationId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.SkipCorrelationId = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreCorrelationId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.SkipCorrelationId = false;

            var context = new DefaultHttpContext();

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreCorrelationIdWithExistingId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.SkipCorrelationId = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("some-value");
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreCallId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.SkipCallId = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreCallId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.SkipCallId = false;

            var context = new DefaultHttpContext();

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be(GuidProvider.StringValue);
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameIsNull_DefaultViaHeaderAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = null;

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/1.1";

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Via).SingleOrDefault().Should().Be("1.1 gwcore");
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNull_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = "some-proxy";

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/2";

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Via).SingleOrDefault().Should().Be("2.0 some-proxy");
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeader_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = "some-proxy";

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/2";
            context.Request.Headers.Add(HeaderNames.Via, "1.0 test");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.0 test", "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeaders_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = "some-proxy";

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/2";
            context.Request.Headers.Add(HeaderNames.Via, "1.0 test, 1.1 test2");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.0 test", "1.1 test2", "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeadersAndIgnoreVia_ViaHeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.UpstreamRequest.Headers.SkipVia = true;

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/2";
            context.Request.Headers.Add(HeaderNames.Via, "1.0 test, 1.1 test2");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderNames.Via).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaHeadersAndIgnoreAllDownstreamHeaders_DownstreamViaHeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var proxy = options.Routes["/api/"].Proxy!;
            proxy.ProxyName = "some-proxy";
            proxy.UpstreamRequest.Headers.DiscardInboundHeaders = true;
            proxy.UpstreamRequest.Headers.SkipVia = false;

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/2";
            context.Request.Headers.Add(HeaderNames.Via, "1.0 test, 1.1 test2");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Via).SingleOrDefault().Should().Be("2.0 some-proxy");
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameNotNullWithExistingViaSeparateHeaders_ViaHeaderHasProxyNameAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = "some-proxy";

            var context = new DefaultHttpContext();
            context.Request.Protocol = "HTTP/2";
            context.Request.Headers.Add(HeaderNames.Via, new[] { "1.0 test", "1.1 test2" });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.0 test", "1.1 test2", "2.0 some-proxy" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameIsNull_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-proxy-name";
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = null;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_RequestHasProxyNameHeader_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-proxy-name";
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = "edge";

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("edge");
        }

        [TestMethod]
        public async Task SetHeadersAsync_RequestHasProxyNameHeaderAndNoProxyNameInSettings_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-proxy-name";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_RequestHasProxyNameHeaderWithProxyNameInSetings_HeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.ProxyName = "new-value";

            const string HeaderName = "x-gwcore-proxy-name";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options: options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("new-value");
        }

        [TestMethod]
        public async Task SetHeadersAsync_HeaderOverrides_HeadersIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.Overrides = new Dictionary<string, string[]>
            {
                ["x-extra-1"] = new[] { "value1_1", "value1_2" },
                ["x-extra-2"] = new[] { "value2_1", "value2_2" },
                ["x-extra-3"] = new string[0], // it should be ignored
                ["x-extra-4"] = null!, // it should be ignored
            };

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("x-extra-0", "value0_0");
            context.Request.Headers.Add("x-extra-2", "value2_0");
            context.Request.Headers.Add("x-extra-3", "value3_0");
            context.Request.Headers.Add("x-extra-4", "value4_0");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues("x-extra-0").Should().BeEquivalentTo(new[] { "value0_0" });
            message.Headers.GetValues("x-extra-1").Should().BeEquivalentTo(new[] { "value1_1", "value1_2" });
            message.Headers.GetValues("x-extra-2").Should().BeEquivalentTo(new[] { "value2_1", "value2_2" });
            message.Headers.GetValues("x-extra-3").Should().BeEquivalentTo(new[] { "value3_0" });
            message.Headers.GetValues("x-extra-4").Should().BeEquivalentTo(new[] { "value4_0" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_HeaderDiscards_HeadersNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.Discards.Add("x-discard-1");
            headersOptions.Discards.Add("x-discard-2");

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("x-keep-1", "value1_0");
            context.Request.Headers.Add("x-discard-1", "value1_0");
            context.Request.Headers.Add("x-discard-2", "value2_0");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains("x-keep-1").Should().BeTrue();
            message.Headers.Contains("x-discard-1").Should().BeFalse();
            message.Headers.Contains("x-discard-2").Should().BeFalse();
        }

        private static async Task<HttpRequestMessage> SetHeadersAsync(
            HttpContext httpContext,
            GatewayOptions? options = null,
            IServiceCollection? services = null)
        {
            services ??= new ServiceCollection();
            var serviceProvider = services.AddTest(gatewayOptions: options).BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IRequestHeaderSetter>();
            var context = serviceProvider.GetProxyContext(httpContext);
            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message, default);
            return message;
        }

        [TestMethod]
        public async Task SetHeadersAsync_HasNonStandardHopByHopHeaders_HeadersNotIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.Connection;
            context.Request.Headers.Add(header, new[] { HeaderNames.KeepAlive, "x-test1", "x-test2, x-test3" });
            context.Request.Headers.Add("x-test1", "some-value");
            context.Request.Headers.Add("x-test2", "some-value");
            context.Request.Headers.Add("x-test3", "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.TryGetValues(HeaderNames.KeepAlive, out _).Should().BeFalse();
            message.Headers.TryGetValues("x-test1", out _).Should().BeFalse();
            message.Headers.TryGetValues("x-test2", out _).Should().BeFalse();
            message.Headers.TryGetValues("x-test3", out _).Should().BeFalse();
        }
    }
}