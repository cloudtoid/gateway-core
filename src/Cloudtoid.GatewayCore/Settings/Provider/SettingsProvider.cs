namespace Cloudtoid.GatewayCore.Settings
{
    using Microsoft.Extensions.Options;
    using static Contract;

    internal sealed class SettingsProvider : ISettingsProvider
    {
        private readonly ISettingsCreator settingsCreator;
        private readonly IOptionsMonitor<GatewayOptions> options;

        public SettingsProvider(
            ISettingsCreator settingsCreator,
            IOptionsMonitor<GatewayOptions> options)
        {
            this.settingsCreator = CheckValue(settingsCreator, nameof(settingsCreator));
            this.options = CheckValue(options, nameof(options));

            options.OnChange(_ => CurrentValue = CreateSettings());
            CurrentValue = CreateSettings();
        }

        public GatewaySettings CurrentValue { get; private set; }

        private GatewaySettings CreateSettings()
            => settingsCreator.Create(options.CurrentValue);
    }
}
