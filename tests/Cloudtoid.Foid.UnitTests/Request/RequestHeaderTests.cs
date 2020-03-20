////namespace Cloudtoid.Foid.UnitTests
////{
////    using System;
////    using System.Collections.Generic;
////    using System.Linq;
////    using System.Net;
////    using System.Net.Http;
////    using System.Threading.Tasks;
////    using Cloudtoid.Foid.Options;
////    using Cloudtoid.Foid.Proxy;
////    using FluentAssertions;
////    using Microsoft.AspNetCore.Http;
////    using Microsoft.Extensions.DependencyInjection;
////    using Microsoft.Net.Http.Headers;
////    using Microsoft.VisualStudio.TestTools.UnitTesting;
////    using NSubstitute;

////    [TestClass]
////    public sealed class RequestHeaderTests
////    {
////        [TestMethod]
////        public async Task GetHostHeaderValue_WhenIgnoreHost_HostHeaderNotIncludedAsync()
////        {
////            // Arrange
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = false;
////            options.Proxy.Upstream.Request.Headers.IgnoreHost = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderNames.Host, new[] { "test-host" });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderNames.Host).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task GetHostHeaderValue_WhenNotIgnoreHostButIgnoreAll_DefaultHostHeaderIncludedAsync()
////        {
////            // Arrange
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = true;
////            options.Proxy.Upstream.Request.Headers.IgnoreHost = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderNames.Host, new[] { "test-host" });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
////        }

////        [TestMethod]
////        public async Task GetHostHeaderValue_WhenHostNameIncludesPortNumber_PortNumberIsRemovedAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderNames.Host, new[] { "host:123", "random-value" });

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
////            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be("host");
////        }

////        [TestMethod]
////        public async Task GetHostHeaderValue_WhenHostHeaderNotSpecified_HostHeaderIsMachineNameAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderNames.Host, Array.Empty<string>());

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
////            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNoHostHeader_HostHeaderIsAddedAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
////            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenHostHeaderIncluded_HostHeaderIsNotAddedAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderNames.Host, "my-host");

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
////            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be("my-host");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenHeaderWithUnderscore_HeaderRemovedAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add("X-Good-Header", "some-value");
////            context.Request.Headers.Add("X_Bad_Header", "some-value");

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.Contains("X-Good-Header").Should().BeTrue();
////            message.Headers.Contains("X_Bad_Header").Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenAllowHeadersWithUnderscore_HeaderKeptAsync()
////        {
////            // Arrange
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.AllowHeadersWithUnderscoreInName = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add("X-Good-Header", "some-value");
////            context.Request.Headers.Add("X_Bad_Header", "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains("X-Good-Header").Should().BeTrue();
////            message.Headers.Contains("X_Bad_Header").Should().BeTrue();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenHeaderWithEmptyValue_HeaderRemovedAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add("X-Empty-Header", string.Empty);

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.Contains("X-Empty-Header").Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenAllowHeaderWithEmptyValue_HeaderIsKeptAsync()
////        {
////            // Arrange
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.AllowHeadersWithEmptyValue = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add("X-Empty-Header", string.Empty);

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains("X-Empty-Header").Should().BeTrue();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenHasContentHeaders_ContentHeadersNotIncludedAsync()
////        {
////            // Arrange
////            var context = new DefaultHttpContext();
////            var header = HeaderNames.ContentType;
////            context.Request.Headers.Add(header, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.TryGetValues(header, out _).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenCustomHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
////        {
////            // Arrange
////            var provider = Substitute.For<IRequestHeaderValuesProvider>();

////            provider
////                .TryGetHeaderValues(
////                    Arg.Any<HttpContext>(),
////                    Arg.Is("X-Keep-Header"),
////                    Arg.Any<IList<string>>(),
////                    out Arg.Any<IList<string>>())
////                .Returns(x =>
////                {
////                    x[3] = new[] { "keep-value" };
////                    return true;
////                });

////            provider
////                .TryGetHeaderValues(
////                    Arg.Any<HttpContext>(),
////                    Arg.Is("X-Drop-Header"),
////                    Arg.Any<IList<string>>(),
////                    out Arg.Any<IList<string>>())
////                .Returns(false);

////            var services = new ServiceCollection()
////                .AddSingleton(provider)
////                .AddTest();

////            var serviceProvider = services.BuildServiceProvider();
////            var setter = serviceProvider.GetRequiredService<IRequestHeaderSetter>();

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add("X-Keep-Header", "keep-value");
////            context.Request.Headers.Add("X-Drop-Header", "drop-value");

////            // Act
////            var message = new HttpRequestMessage();
////            await setter.SetHeadersAsync(context, message, default);

////            // Assert
////            message.Headers.Contains("X-Keep-Header").Should().BeTrue();
////            message.Headers.GetValues("X-Keep-Header").Should().BeEquivalentTo(new[] { "keep-value" });
////            message.Headers.Contains("X-Drop-Header").Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenHasXForwardHeaders_ExistingHeadersAreIgnoredAsync()
////        {
////            // Arrange
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedFor = false;
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedHost = false;
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedProtocol = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(Headers.Names.ForwardedFor, "some-value");
////            context.Request.Headers.Add(Headers.Names.ForwardedHost, "some-value");
////            context.Request.Headers.Add(Headers.Names.ForwardedProtocol, "some-value");
////            context.Request.Host = new HostString("some-host");
////            context.Request.Scheme = "HTTPS";
////            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(Headers.Names.ForwardedFor).Should().BeEquivalentTo(new[] { "0.1.2.3" });
////            message.Headers.GetValues(Headers.Names.ForwardedHost).Should().BeEquivalentTo(new[] { "some-host" });
////            message.Headers.GetValues(Headers.Names.ForwardedProtocol).Should().BeEquivalentTo(new[] { "HTTPS" });
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIncludeExternalAddress_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-foid-external-address";

////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IncludeExternalAddress = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "3.2.1.0");
////            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("0.1.2.3");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIncludeExternalAddress_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-foid-external-address";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IncludeExternalAddress = false;

////            var context = new DefaultHttpContext();
////            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreAllDownstreamHeaders_NoDownstreamHeaderIsIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-custom-test";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreAllDownstreamHeaders_DownstreamHeadersAreIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-custom-test";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, new[] { "first-value", "second-value" });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).Should().BeEquivalentTo(new[] { "first-value", "second-value" });
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreAllDownstreamHeadersAndCorrelationId_NewCorrelationIdIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-correlation-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = true;
////            options.Proxy.Upstream.Request.Headers.IgnoreCorrelationId = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers
////                .GetValues(HeaderName)
////                .SingleOrDefault()
////                .Should()
////                .Be(GuidProvider.StringValue);
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreAllDownstreamHeadersAndCorrelationId_ExistingCorrelationIdIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-correlation-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = false;
////            options.Proxy.Upstream.Request.Headers.IgnoreCorrelationId = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers
////                .GetValues(HeaderName)
////                .SingleOrDefault()
////                .Should()
////                .Be("some-value");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreForwardedFor_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-for";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedFor = true;

////            var context = new DefaultHttpContext();
////            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreForwardedFor_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-for";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedFor = false;

////            var context = new DefaultHttpContext();
////            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("0.1.2.3");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreForwardedForButHasValue_OldValueIsIgnoredAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-for";

////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedFor = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "3.2.1.0");
////            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).Should().BeEquivalentTo(new[] { "0.1.2.3" });
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreForwardedProtocol_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-proto";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedProtocol = true;

////            var context = new DefaultHttpContext();
////            context.Request.Scheme = "HTTPS";

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreForwardedProtocol_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-proto";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedProtocol = false;

////            var context = new DefaultHttpContext();
////            context.Request.Scheme = "HTTPS";

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("HTTPS");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreForwardedHost_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-host";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedHost = true;

////            var context = new DefaultHttpContext();
////            context.Request.Scheme = "HTTPS";

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreForwardedHost_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-forwarded-host";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreForwardedHost = false;

////            var context = new DefaultHttpContext();
////            context.Request.Host = new HostString("some-host");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("some-host");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreCorrelationId_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-correlation-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreCorrelationId = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreCorrelationId_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-correlation-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreCorrelationId = false;

////            var context = new DefaultHttpContext();

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers
////                .GetValues(HeaderName)
////                .SingleOrDefault()
////                .Should()
////                .Be(GuidProvider.StringValue);
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreCorrelationIdWithExistingId_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-correlation-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreCorrelationId = false;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("some-value");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenIgnoreCallId_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-call-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreCallId = true;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenNotIgnoreCallId_HeaderIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-call-id";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.IgnoreCallId = false;

////            var context = new DefaultHttpContext();

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers
////                .GetValues(HeaderName)
////                .SingleOrDefault()
////                .Should()
////                .Be(GuidProvider.StringValue);
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenProxyNameIsEmpty_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-foid-proxy-name";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.ProxyName = string.Empty;

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.Contains(HeaderName).Should().BeFalse();
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenProxyNameSpecified_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-foid-proxy-name";
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.ProxyName = "edge";

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("edge");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenProxyNameDefault_HeaderNotIncludedAsync()
////        {
////            // Arrange
////            const string HeaderName = "x-foid-proxy-name";
////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add(HeaderName, "some-value");

////            // Act
////            var message = await SetHeadersAsync(context);

////            // Assert
////            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("foid");
////        }

////        [TestMethod]
////        public async Task SetHeadersAsync_WhenExtraHeaders_HeadersIncludedAsync()
////        {
////            // Arrange
////            var options = new FoidOptions();
////            options.Proxy.Upstream.Request.Headers.Headers = new[]
////            {
////                new ExtraHeader
////                {
////                    Name = "x-extra-1",
////                    Values = new[] { "value1_1", "value1_2" }
////                },
////                new ExtraHeader
////                {
////                    Name = "x-extra-2",
////                    Values = new[] { "value2_1", "value2_2" }
////                },
////                new ExtraHeader
////                {
////                    Name = "x-extra-3",
////                    Values = new string[0]
////                },
////                new ExtraHeader
////                {
////                    Name = "x-extra-4",
////                    Values = null
////                },
////                new ExtraHeader
////                {
////                    Name = null,
////                    Values = null
////                },
////            };

////            var context = new DefaultHttpContext();
////            context.Request.Headers.Add("x-extra-2", "value2_0");

////            // Act
////            var message = await SetHeadersAsync(context, options);

////            // Assert
////            message.Headers.GetValues("x-extra-1").Should().BeEquivalentTo(new[] { "value1_1", "value1_2" });
////            message.Headers.GetValues("x-extra-2").Should().BeEquivalentTo(new[] { "value2_1", "value2_2" });
////            message.Headers.Contains("x-extra-3").Should().BeFalse();
////            message.Headers.Contains("x-extra-4").Should().BeFalse();
////        }

////        private static async Task<HttpRequestMessage> SetHeadersAsync(
////            HttpContext context,
////            FoidOptions? options = null)
////        {
////            var services = new ServiceCollection().AddTest(options);
////            var serviceProvider = services.BuildServiceProvider();
////            var setter = serviceProvider.GetRequiredService<IRequestHeaderSetter>();
////            var message = new HttpRequestMessage();
////            await setter.SetHeadersAsync(context, message, default);
////            return message;
////        }
////    }
////}