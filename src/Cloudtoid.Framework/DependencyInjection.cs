namespace Cloudtoid
{
    using Microsoft.Extensions.DependencyInjection;
    using static Contract;

    public static class DependencyInjection
    {
        public static IServiceCollection AddFramework(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<RegistrationTag>())
                return services;

            return services
                .TryAddSingleton<RegistrationTag>();
        }

        private sealed class RegistrationTag
        {
            internal bool IsRegistered { get; set; }
        }
    }
}
