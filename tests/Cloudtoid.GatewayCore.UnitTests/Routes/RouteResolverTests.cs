using Cloudtoid.GatewayCore.Routes;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed class RouteResolverTests
    {
        private readonly IRouteResolver resolver;

        public RouteResolverTests()
        {
            var services = new ServiceCollection().AddTest();
            var serviceProvider = services.BuildServiceProvider();
            resolver = serviceProvider.GetRequiredService<IRouteResolver>();
        }

        [TestMethod]
        public void TryResolve_SimpleMatch_Success()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/test/";

            // Act
            resolver.TryResolve(httpContext, out var route).Should().BeTrue();

            // Assert
            route.Should().NotBeNull();
            route!.PathSuffix.Should().Be("test/");
        }

        [TestMethod]
        public void TryResolve_RouteSeenBefore_ItemIsReadFromCache()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/";

            // Act
            resolver.TryResolve(httpContext, out var route1).Should().BeTrue();
            resolver.TryResolve(httpContext, out var route2).Should().BeTrue();

            httpContext.Request.Path = "/api/test/";
            resolver.TryResolve(httpContext, out var route3).Should().BeTrue();

            // Assert
            route1.Should().NotBeNull();
            route2.Should().NotBeNull();
            ReferenceEquals(route1, route2).Should().BeTrue();

            route3.Should().NotBeNull();
            ReferenceEquals(route1, route3).Should().BeFalse();
        }

        [TestMethod]
        public void TryResolve_RouteNotFound_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/ipa/";

            // Act
            resolver.TryResolve(httpContext, out var route).Should().BeFalse();

            // Assert
            route.Should().BeNull();
        }
    }
}
