﻿namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Upstream;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public sealed class RequestHeaderTests
    {
        private const string ForwardedHeader = "Forwarded";
        private const string XForwardedForHeader = "x-forwarded-for";
        private const string XForwardedHostHeader = "x-forwarded-host";
        private const string XForwardedProtoHeader = "x-forwarded-proto";
        private static readonly IPAddress IpV4Sample = new IPAddress(new byte[] { 0, 1, 2, 3 });
        private static readonly IPAddress IpV4Sample2 = new IPAddress(new byte[] { 4, 5, 6, 7 });
        private static readonly IPAddress IpV6Sample = new IPAddress(new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 });

        [TestMethod]
        public async Task GetHostHeaderValue_IgnoreHost_HostHeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreAllDownstreamHeaders = false;
            headersOptions.IgnoreHost = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, new[] { "test-host" });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetHostHeaderValue_NotIgnoreHostButIgnoreAll_DefaultHostHeaderIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreAllDownstreamHeaders = true;
            headersOptions.IgnoreHost = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, new[] { "test-host" });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public async Task GetHostHeaderValue_HostNameIncludesPortNumber_PortNumberIsRemovedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, new[] { "host:123", "random-value" });

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be("host");
        }

        [TestMethod]
        public async Task GetHostHeaderValue_HostHeaderNotSpecified_HostHeaderIsMachineNameAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, Array.Empty<string>());

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public async Task SetHeadersAsync_NoHostHeader_HostHeaderIsAddedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public async Task SetHeadersAsync_HostHeaderIncluded_HostHeaderIsNotAddedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, "my-host");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be("my-host");
        }

        [TestMethod]
        public async Task SetHeadersAsync_HeaderWithUnderscore_HeaderRemovedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains("X-Good-Header").Should().BeTrue();
            message.Headers.Contains("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_AllowHeadersWithUnderscore_HeaderKeptAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.AllowHeadersWithUnderscoreInName = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains("X-Good-Header").Should().BeTrue();
            message.Headers.Contains("X_Bad_Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_HeaderWithEmptyValue_HeaderRemovedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains("X-Empty-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_AllowHeaderWithEmptyValue_HeaderIsKeptAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.AllowHeadersWithEmptyValue = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains("X-Empty-Header").Should().BeTrue();
        }

        [TestMethod]
        public async Task SetHeadersAsync_HasContentHeaders_ContentHeadersNotIncludedAsync()
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
        public async Task SetHeadersAsync_CustomHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            // Arrange
            var provider = Substitute.For<IRequestHeaderValuesProvider>();
            var services = new ServiceCollection().AddSingleton(provider);

            provider
                .TryGetHeaderValues(
                    Arg.Any<ProxyContext>(),
                    Arg.Is("X-Keep-Header"),
                    Arg.Any<IList<string>>(),
                    out Arg.Any<IList<string>?>())
                .Returns(x =>
                {
                    x[3] = new[] { "keep-value" };
                    return true;
                });

            provider
                .TryGetHeaderValues(
                    Arg.Any<ProxyContext>(),
                    Arg.Is("X-Drop-Header"),
                    Arg.Any<IList<string>>(),
                    out Arg.Any<IList<string>?>())
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
        public async Task SetHeadersAsync_IncludeExternalAddress_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-external-address";

            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IncludeExternalAddress = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "3.2.1.0");
            context.Connection.RemoteIpAddress = IpV4Sample;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be(IpV4Sample.ToString());
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIncludeExternalAddress_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-external-address";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IncludeExternalAddress = false;

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
            headersOptions.IgnoreAllDownstreamHeaders = true;

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
            headersOptions.IgnoreAllDownstreamHeaders = false;

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
            headersOptions.IgnoreAllDownstreamHeaders = true;
            headersOptions.IgnoreCorrelationId = false;

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
            headersOptions.IgnoreAllDownstreamHeaders = false;
            headersOptions.IgnoreCorrelationId = false;

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
        public async Task SetHeadersAsync_HasXForwardHeaders_ExistingHeadersAreIgnoredAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = false;
            headersOptions.UseXForwarded = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(Headers.Names.ForwardedFor, "some-value");
            context.Request.Headers.Add(Headers.Names.ForwardedHost, "some-value");
            context.Request.Headers.Add(Headers.Names.ForwardedProtocol, "some-value");
            context.Request.Host = new HostString("some-host");
            context.Request.Scheme = "HTTPS";
            context.Connection.RemoteIpAddress = IpV4Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(Headers.Names.ForwardedFor).Should().BeEquivalentTo(new[] { IpV4Sample.ToString() });
            message.Headers.GetValues(Headers.Names.ForwardedHost).Should().BeEquivalentTo(new[] { "some-host" });
            message.Headers.GetValues(Headers.Names.ForwardedProtocol).Should().BeEquivalentTo(new[] { "HTTPS" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreForwarded_HeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = true;
            headersOptions.UseXForwarded = false;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV4Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(XForwardedForHeader).Should().BeFalse();
            message.Headers.Contains(ForwardedHeader).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreForwardedWithXForwarded_HeaderNotIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = true;
            headersOptions.UseXForwarded = true;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV4Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(XForwardedForHeader).Should().BeFalse();
            message.Headers.Contains(ForwardedHeader).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreForwarded_HeaderIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = false;
            headersOptions.UseXForwarded = false;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV4Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;
            context.Request.Host = new HostString("some-host");
            context.Request.Scheme = "http";

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(ForwardedHeader).SingleOrDefault().Should().Be("by=4.5.6.7;for=0.1.2.3;host=some-host;proto=http");
            message.Headers.Contains(XForwardedForHeader).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_NotIgnoreForwardedWithXForwarded_HeaderIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = false;
            headersOptions.UseXForwarded = true;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV4Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;
            context.Request.Host = new HostString("some-host");
            context.Request.Scheme = "http";

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(XForwardedForHeader).SingleOrDefault().Should().Be(IpV4Sample.ToString());
            message.Headers.GetValues(XForwardedHostHeader).SingleOrDefault().Should().Be("some-host");
            message.Headers.GetValues(XForwardedProtoHeader).SingleOrDefault().Should().Be("http");
        }

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreForwardedForButHasValue_OldValueIsIgnoredAsync()
        {
            // Arrange
            const string HeaderName = XForwardedForHeader;

            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = false;
            headersOptions.UseXForwarded = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "3.2.1.0");
            context.Connection.RemoteIpAddress = IpV4Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).Should().BeEquivalentTo(new[] { IpV4Sample.ToString() });
        }

        [TestMethod]
        public async Task SetHeadersAsync_IpAddressV6InXForwarded_IsNotWrappedInBracketsAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = false;
            headersOptions.UseXForwarded = true;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV6Sample;
            context.Connection.LocalIpAddress = IpV4Sample2;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(XForwardedForHeader).Should().BeEquivalentTo(new[] { "1020:3040:5060:7080:9010:1112:1314:1516" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_IpAddressV6InForwarded_IsWrappedInBracketsAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreForwarded = false;
            headersOptions.UseXForwarded = false;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IpV6Sample;
            context.Connection.LocalIpAddress = IpV6Sample;

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(ForwardedHeader).Should().BeEquivalentTo(new[] { "by=\"[1020:3040:5060:7080:9010:1112:1314:1516]\";for=\"[1020:3040:5060:7080:9010:1112:1314:1516]\"" });
        }

        ////[TestMethod]
        ////public async Task SetHeadersAsync_IgnoreForwardedProtocol_HeaderNotIncludedAsync()
        ////{
        ////    // Arrange
        ////    const string HeaderName = "x-forwarded-proto";
        ////    var options = TestExtensions.CreateDefaultOptions();
        ////    var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
        ////    headersOptions.UseXForwarded = true;

        ////    var context = new DefaultHttpContext();
        ////    context.Request.Scheme = "HTTPS";

        ////    // Act
        ////    var message = await SetHeadersAsync(context, options);

        ////    // Assert
        ////    message.Headers.Contains(HeaderName).Should().BeFalse();
        ////}

        ////[TestMethod]
        ////public async Task SetHeadersAsync_NotIgnoreForwardedProtocol_HeaderIncludedAsync()
        ////{
        ////    // Arrange
        ////    const string HeaderName = "x-forwarded-proto";
        ////    var options = TestExtensions.CreateDefaultOptions();
        ////    var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
        ////    headersOptions.UseXForwarded = false;

        ////    var context = new DefaultHttpContext();
        ////    context.Request.Scheme = "HTTPS";

        ////    // Act
        ////    var message = await SetHeadersAsync(context, options);

        ////    // Assert
        ////    message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("HTTPS");
        ////}

        ////[TestMethod]
        ////public async Task SetHeadersAsync_IgnoreForwardedHost_HeaderNotIncludedAsync()
        ////{
        ////    // Arrange
        ////    const string HeaderName = "x-forwarded-host";
        ////    var options = TestExtensions.CreateDefaultOptions();
        ////    var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
        ////    headersOptions.IgnoreForwardedHost = true;

        ////    var context = new DefaultHttpContext();
        ////    context.Request.Scheme = "HTTPS";

        ////    // Act
        ////    var message = await SetHeadersAsync(context, options);

        ////    // Assert
        ////    message.Headers.Contains(HeaderName).Should().BeFalse();
        ////}

        ////[TestMethod]
        ////public async Task SetHeadersAsync_NotIgnoreForwardedHost_HeaderIncludedAsync()
        ////{
        ////    // Arrange
        ////    const string HeaderName = "x-forwarded-host";
        ////    var options = TestExtensions.CreateDefaultOptions();
        ////    var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
        ////    headersOptions.IgnoreForwardedHost = false;

        ////    var context = new DefaultHttpContext();
        ////    context.Request.Host = new HostString("some-host");

        ////    // Act
        ////    var message = await SetHeadersAsync(context, options);

        ////    // Assert
        ////    message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("some-host");
        ////}

        [TestMethod]
        public async Task SetHeadersAsync_IgnoreCorrelationId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-correlation-id";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.IgnoreCorrelationId = true;

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
            headersOptions.IgnoreCorrelationId = false;

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
            headersOptions.IgnoreCorrelationId = false;

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
            headersOptions.IgnoreCallId = true;

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
            headersOptions.IgnoreCallId = false;

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
        public async Task SetHeadersAsync_ProxyNameIsEmpty_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-proxy-name";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.ProxyName = string.Empty;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameSpecified_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-proxy-name";
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.ProxyName = "edge";

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("edge");
        }

        [TestMethod]
        public async Task SetHeadersAsync_ProxyNameDefault_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-gwcore-proxy-name";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("gwcore");
        }

        [TestMethod]
        public async Task SetHeadersAsync_ExtraHeaders_HeadersIncludedAsync()
        {
            // Arrange
            var options = TestExtensions.CreateDefaultOptions();
            var headersOptions = options.Routes["/api/"].Proxy!.UpstreamRequest.Headers;
            headersOptions.Overrides = new Dictionary<string, string[]>()
            {
                ["x-extra-1"] = new[] { "value1_1", "value1_2" },
                ["x-extra-2"] = new[] { "value2_1", "value2_2" },
                ["x-extra-3"] = new string[0],
                ["x-extra-4"] = null!,
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
            message.Headers.Contains("x-extra-3").Should().BeFalse(); // should have been removed because its new value is empty
            message.Headers.Contains("x-extra-4").Should().BeFalse(); // should have been removed because its new value is null
        }

        private static async Task<HttpRequestMessage> SetHeadersAsync(
            HttpContext httpContext,
            GatewayOptions? options = null,
            IServiceCollection? services = null)
        {
            services ??= new ServiceCollection();
            var serviceProvider = services.AddTest().AddTestOptions(options).BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IRequestHeaderSetter>();
            var context = serviceProvider.GetProxyContext(httpContext);
            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message, default);
            return message;
        }
    }
}