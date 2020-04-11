namespace Cloudtoid.GatewayCore.UnitTests
{
    using Cloudtoid.GatewayCore.Routes;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class RouteResolverTests
    {
        private readonly IRouteResolver resolver;

        public RouteResolverTests()
        {
            var services = new ServiceCollection().AddTest().AddTestOptions();
            var serviceProvider = services.BuildServiceProvider();
            resolver = serviceProvider.GetRequiredService<IRouteResolver>();
        }

        [TestMethod]
        public void TryResolve_WhenRouteSeenBefore_ItemIsReadFromCache()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/";

            // Act
            resolver.TryResolve(httpContext, out var route1);
            resolver.TryResolve(httpContext, out var route2);

            httpContext.Request.Path = "/api/test/";
            resolver.TryResolve(httpContext, out var route3);

            // Assert
            route1.Should().NotBeNull();
            route2.Should().NotBeNull();
            ReferenceEquals(route1, route2).Should().BeTrue();

            route3.Should().NotBeNull();
            ReferenceEquals(route1, route3).Should().BeFalse();
        }
    }
}
