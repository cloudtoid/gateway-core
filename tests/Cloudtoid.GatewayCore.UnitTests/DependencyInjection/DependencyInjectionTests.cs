using System;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed class DependencyInjectionTests
    {
        [TestMethod]
        public void UseGatewayCore_AddNotCalled_ExceptionIsThrown()
        {
            var provider = new ServiceCollection().BuildServiceProvider();
            var builder = Substitute.For<IApplicationBuilder>();
            builder.ApplicationServices.Returns(provider);

            Action act = () => builder.UseGatewayCore();
            act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Call AddGatewayCore before calling UseGatewayCore");
        }
    }
}
