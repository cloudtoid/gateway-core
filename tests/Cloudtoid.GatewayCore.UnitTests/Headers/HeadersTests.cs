using Cloudtoid.GatewayCore.Headers;
using FluentAssertions;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed class HeadersTests
    {
        [TestMethod]
        public void IsStandardHopByHopHeader_ValidHopByHopHeaders_ItIs()
        {
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.KeepAlive).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.TransferEncoding).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.TE).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.Connection).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.Upgrade).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.ProxyAuthenticate).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.ProxyAuthorization).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.Trailer).Should().BeFalse();
        }
    }
}
