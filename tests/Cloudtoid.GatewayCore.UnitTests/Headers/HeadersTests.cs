namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
    using System.Net;
    using System.Reflection;
    using Cloudtoid.GatewayCore.Headers;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class HeadersTests
    {
        [TestMethod]
        public void IsCustomHeader_StartsWithX_ItIs()
        {
            HeaderTypes.IsCustomHeader("x-test").Should().BeTrue();
        }

        [TestMethod]
        public void IsCustomHeader_NotStartWithX_ItIsNot()
        {
            HeaderTypes.IsCustomHeader("y-test").Should().BeFalse();
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

        [TestMethod]
        public void AddOrAppendHeaderValues_HeaderAlreadyExists_AppendsHeaderValue()
        {
            var headers = new HeaderDictionary();
            headers.AddOrAppendHeaderValues("test", new[] { "value" });
            headers.AddOrAppendHeaderValues("test", new[] { "new-value" });

            headers["test"].Should().BeEquivalentTo(new[] { "value", "new-value" });
        }
    }
}
