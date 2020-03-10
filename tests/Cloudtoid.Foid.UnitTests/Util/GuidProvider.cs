namespace Cloudtoid.Foid.UnitTests
{
    using System;

    internal sealed class GuidProvider : IGuidProvider
    {
        private GuidProvider()
        {
        }

#pragma warning disable RS0030 // Do not used banned APIs
        internal static Guid Value { get; } = Guid.NewGuid();
#pragma warning restore RS0030 // Do not used banned APIs

        internal static IGuidProvider Instance { get; } = new GuidProvider();

        public Guid NewGuid() => Value;
    }
}
