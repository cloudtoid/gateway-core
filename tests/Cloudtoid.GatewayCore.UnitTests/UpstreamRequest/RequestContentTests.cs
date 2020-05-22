namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore;
    using Cloudtoid.GatewayCore.Upstream;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class RequestContentTests
    {
        private IServiceProvider? serviceProvider;

        [TestMethod]
        public async Task SetContentAsync_HasContentHeaders_ContentHeadersIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentDisposition;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out var headers).Should().BeTrue();
            headers.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task SetContentAsync_HasCustomContentHeader_ContentHeaderIsIgnoredAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = "x-test-header";
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_HasContentTypeHeader_ContentTypeHeadersIncludedOnlyOnceAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentType;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out var headers).Should().BeTrue();
            headers.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task SetContentAsync_IgnoreHeaders_ContentHeadersNotIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentLocation;
            context.Request.Headers.Add(header, "some-value");

            var options = TestExtensions.CreateDefaultOptions();
            options.Routes["/api/"].Proxy!.UpstreamRequest.Headers.IgnoreAllDownstreamHeaders = true;

            // Act
            var message = await SetContentAsync(context, options: options);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_NoContentHeader_NoContentHeaderAddedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.Accept;
            context.Request.Headers.Add(header, "some-value");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Content.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetContentAsync_BodyIsNull_LogsDebugAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Body = null;

            // Act
            await SetContentAsync(context);

            // Assert
            var logger = (Logger<RequestContentSetter>)serviceProvider.GetRequiredService<ILogger<RequestContentSetter>>();
            logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase("The inbound downstream request does not have a content body")).Should().BeTrue();
        }

        [TestMethod]
        public async Task SetContentAsync_BodyNotReadable_ThrowsAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("some-value"));
            await context.Request.Body.DisposeAsync();

            // Act
            Func<Task> act = () => SetContentAsync(context);

            // Assert
            await act.Should().ThrowExactlyAsync<InvalidOperationException>("*The inbound downstream request does not have a readable body*");
        }

        [TestMethod]
        public async Task SetContentAsync_BodyNotAtPositionZeroButSeekable_LogsDebugAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var body = context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("some-value"));
            body.Position = 1;

            // Act
            await SetContentAsync(context);

            // Assert
            var logger = (Logger<RequestContentSetter>)serviceProvider.GetRequiredService<ILogger<RequestContentSetter>>();
            logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase("The inbound downstream request has a seek-able body stream. Resetting the stream to the beginning.")).Should().BeTrue();
        }

        [TestMethod]
        public async Task SetContentAsync_NoContentLength_LogsDebugAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            await SetContentAsync(context, contentLength: null);

            // Assert
            var logger = (Logger<RequestContentSetter>)serviceProvider.GetRequiredService<ILogger<RequestContentSetter>>();
            logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase("The inbound downstream request does not specify a 'Content-Length'.")).Should().BeTrue();
        }

        [TestMethod]
        public async Task SetContentAsync_HeaderRejectedByIRequestContentHeaderValuesProvider_LogsInfoAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.ContentMD5, "Q2hlY2sgSW50ZWdyaXR5IQ==");
            var provider = new DropContentHeaderValuesProvider();

            // Act
            await SetContentAsync(context, provider: provider);

            // Assert
            var logger = (Logger<RequestContentSetter>)serviceProvider.GetRequiredService<ILogger<RequestContentSetter>>();
            logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase("Header 'Content-MD5' is not added. This was instructed by IRequestContentHeaderValuesProvider.TryGetHeaderValues.")).Should().BeTrue();
        }

        private async Task<HttpRequestMessage> SetContentAsync(
            HttpContext httpContext,
            long? contentLength = 10,
            GatewayOptions? options = null,
            IRequestContentHeaderValuesProvider? provider = null)
        {
            var services = new ServiceCollection();

            if (provider != null)
                services.TryAddSingleton(provider);

            services.AddTest().AddTestOptions(options);
            serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IRequestContentSetter>();
            var context = serviceProvider.GetProxyContext(httpContext);
            context.Request.ContentLength = contentLength;
            var message = new HttpRequestMessage();
            await setter.SetContentAsync(context, message, default);
            return message;
        }

        private sealed class DropContentHeaderValuesProvider : RequestContentHeaderValuesProvider
        {
            public override bool TryGetHeaderValues(
                ProxyContext context,
                string name,
                StringValues downstreamValues,
                out StringValues upstreamValues)
            {
                if (name.EqualsOrdinalIgnoreCase(HeaderNames.ContentMD5))
                    return false;

                return base.TryGetHeaderValues(context, name, downstreamValues, out upstreamValues);
            }
        }

        private sealed class NonSeekableStream : Stream
        {
            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => 10;

            public override long Position { get => 10; set => throw new NotImplementedException(); }

            public override void Flush() => throw new NotImplementedException();

            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

            public override void SetLength(long value) => throw new NotImplementedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }
    }
}
