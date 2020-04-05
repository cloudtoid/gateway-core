namespace Cloudtoid.GatewayCore.Settings
{
    using Microsoft.Extensions.Options;
    using static Contract;

    internal sealed class SettingsProvider : ISettingsProvider
    {
        private readonly ISettingsCreator settingsCreator;
        private readonly IOptionsMonitor<ReverseProxyOptions> options;

        public SettingsProvider(
            ISettingsCreator settingsCreator,
            IOptionsMonitor<ReverseProxyOptions> options)
        {
            this.settingsCreator = CheckValue(settingsCreator, nameof(settingsCreator));
            this.options = CheckValue(options, nameof(options));

            options.OnChange(_ => CurrentValue = CreateSettings());
            CurrentValue = CreateSettings();
        }

        public ReverseProxySettings CurrentValue { get; private set; }

        private ReverseProxySettings CreateSettings()
            => settingsCreator.Create(options.CurrentValue);
    }
}
