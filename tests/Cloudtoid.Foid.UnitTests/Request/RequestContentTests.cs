namespace Cloudtoid.Foid.UnitTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RequestContentTests
    {
        [TestMethod]
        public async Task SetContentAsync_WhenHasContentHeaders_ContentHeadersIncludedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var header = HeaderNames.ContentType;
            context.Request.Headers.Add(header, "somevalue");

            // Act
            var message = await SetContentAsync(context);

            // Assert
            message.Headers.TryGetValues(header, out _).Should().BeFalse();
        }

        private static async Task<HttpRequestMessage> SetContentAsync(HttpContext context, FoidOptions? options = null)
        {
            var services = new ServiceCollection().AddTestFramework(options);
            var serviceProvider = services.BuildServiceProvider();
            var setter = serviceProvider.GetRequiredService<IRequestContentSetter>();
            var message = new HttpRequestMessage();
            await setter.SetContentAsync(context, message);
            return message;
        }
    }
}
