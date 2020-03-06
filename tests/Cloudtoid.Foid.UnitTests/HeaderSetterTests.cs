namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HeaderSetterTests
    {
        [TestMethod]
        public void GetHostHeaderValue_WhenHostNameIncludesPortNumber_PortNumberIsRemoved()
        {
            var provider = new RequestHeaderValuesProvider();
            provider.TryGetHeaderValues(new DefaultHttpContext(), HeaderNames.Host, new StringValues(new[] { "host:123", "random-value" }), out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            values[0].Should().Be("host");
        }

        [TestMethod]
        public void GetHostHeaderValue_WhenHostHeaderNotSpecified_HostHeaderIsMachineName()
        {
            var provider = new RequestHeaderValuesProvider();
            provider.TryGetHeaderValues(new DefaultHttpContext(), HeaderNames.Host, Array.Empty<string>(), out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            values[0].Should().Be(Environment.MachineName);
        }
    }
}