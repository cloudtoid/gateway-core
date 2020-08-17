using System;
using System.Net;
using System.Reflection;
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
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.Trailer).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.Upgrade).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.ProxyAuthenticate).Should().BeTrue();
            HeaderTypes.IsStandardHopByHopHeader(HeaderNames.ProxyAuthorization).Should().BeTrue();
        }

        [TestMethod]
        public void IsNonTrailingHeader_Trailer_ItIs()
        {
            HeaderTypes.IsNonTrailingHeader(HeaderNames.Trailer).Should().BeTrue();
        }

        [TestMethod]
        public void IsRequestHeader_ValidRequestHeaders_ItIs()
        {
            const BindingFlags Flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static;
            foreach (var fieldName in Enum.GetNames(typeof(HttpRequestHeader)))
            {
                var headerName = (string)typeof(HeaderNames).GetField(fieldName, Flags)!.GetValue(null)!;
                HeaderTypes.IsRequestHeader(headerName).Should().BeTrue();
            }
        }

        [TestMethod]
        public void IsRequestHeader_ValidResponseHeaders_ItIs()
        {
            const BindingFlags Flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static;
            foreach (var fieldName in Enum.GetNames(typeof(HttpResponseHeader)))
            {
                var headerName = (string)typeof(HeaderNames).GetField(fieldName, Flags)!.GetValue(null)!;
                HeaderTypes.IsResponseHeader(headerName).Should().BeTrue();
            }
        }
    }
}
