namespace Cloudtoid.GatewayCore.UnitTests
{
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static Cloudtoid.GatewayCore.Upstream.RequestHeaderSetter;

    public sealed partial class RequestHeaderTests
    {
        private const string ForwardedHeader = "Forwarded";
        private const string XForwardedForHeader = "x-forwarded-for";
        private const string XForwardedHostHeader = "x-forwarded-host";
        private const string XForwardedProtoHeader = "x-forwarded-proto";
        private static readonly IPAddress IpV4Sample = new IPAddress(new byte[] { 0, 1, 2, 3 });
        private static readonly IPAddress IpV4Sample2 = new IPAddress(new byte[] { 4, 5, 6, 7 });
        private static readonly IPAddress IpV6Sample = new IPAddress(new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 });

        [TestMethod]
        public void TryParseForwardedHeaderValues_SingleValue_Mix()
        {
            TryParseForwardedHeaderValues(string.Empty, out var values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues(" ", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues(",", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues(", , ,    ,", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues(";, , ;,  ;  ,", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("abc", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("host=abc", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue(host: "abc"));

            TryParseForwardedHeaderValues("for=192.0.2.60", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue(@for: "192.0.2.60"));

            TryParseForwardedHeaderValues("proto=http", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue(proto: "http"));

            TryParseForwardedHeaderValues("by=203.0.113.43", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue(by: "203.0.113.43"));

            TryParseForwardedHeaderValues("for=192.0.2.60;proto=http;by=203.0.113.43;host=abc", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "abc", "http"));

            TryParseForwardedHeaderValues("FOR=192.0.2.60;PROTO=http;BY=203.0.113.43;HOST=abc", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "abc", "http"));

            TryParseForwardedHeaderValues(";;;for=192.0.2.60;proto=http;by=203.0.113.43;host=abc;;;;", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "abc", "http"));

            TryParseForwardedHeaderValues(",,,for=192.0.2.60;;proto=http;by=203.0.113.43;;host=abc,,,,", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "abc", "http"));

            TryParseForwardedHeaderValues(" for= 192.0.2.60   ; proto= http ; by= 203.0.113.43 ; host= abc ", out values).Should().BeTrue();
            values.Should().HaveCount(1);
            values![0].Should().Be(new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "abc", "http"));

            TryParseForwardedHeaderValues("host=", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("for=", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("proto=", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("by=", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("host= ", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("for=  ", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("proto= ", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("by= ", out values).Should().BeFalse();
            values.Should().BeNull();

            TryParseForwardedHeaderValues("for=192.0.2.60,proto=http,by=203.0.113.43,host=abc", out values).Should().BeTrue();
            values.Should().HaveCount(4);
            values.Should().BeEquivalentTo(new[]
            {
                new ForwardedHeaderValue(@for: "192.0.2.60"),
                new ForwardedHeaderValue(proto: "http"),
                new ForwardedHeaderValue(by: "203.0.113.43"),
                new ForwardedHeaderValue(host: "abc")
            });

            TryParseForwardedHeaderValues("for=192.0.2.60;proto=http;by=203.0.113.43;host=abc,for=192.0.2.60;proto=https;by=203.0.113.43;host=efg", out values).Should().BeTrue();
            values.Should().HaveCount(2);
            values.Should().BeEquivalentTo(new[]
            {
                new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "abc", "http"),
                new ForwardedHeaderValue("203.0.113.43", "192.0.2.60", "efg", "https")
            });
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
    }
}