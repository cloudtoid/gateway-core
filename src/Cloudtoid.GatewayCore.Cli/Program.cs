namespace Cloudtoid.GatewayCore.Cli
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Extensions.CommandLineUtils;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var app = SetupCommandLineApplication();
            Execute(app, args);
        }

        private static CommandLineApplication SetupCommandLineApplication()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var app = new CommandLineApplication
            {
                Name = assembly.GetName().Name,
                Description = "Cloudtoid Gateway Core CLI"
            };
            app.HelpOption("-?|-h|--help");

            var version = StringUtil.FormatInvariant("Version {0}", assembly.GetName().Version);
            app.VersionOption("-v|--version", version);

            var defaultCommand = app.Command("default", (command) =>
            {
                command.Description = "Run the CLI in its default mode.";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(async () =>
                {
                    await Modes.Default.Startup.StartAsync();
                    Console.ReadKey(false);
                    return 0;
                });
            });

            app.Command("functional-test", (command) =>
            {
                command.Description = "Run in the functional-test mode.";
                command.ExtendedHelpText = "This will run a specific proxy server that is used by the functional tests.";
                command.HelpOption("-?|-h|--help");

                var proxyPortOption = command.Option(
                    "-pp|--proxy-port <port>",
                    $"The port that the proxy is listening on. The default port is {Modes.FunctionalTest.OptionDefaults.ProxyPort}",
                    CommandOptionType.SingleValue);

                var upstreamPortOption = command.Option(
                    "-up|--upstream-port <port>",
                    $"The port that the upstream origin server is listening on. The default port is {Modes.FunctionalTest.OptionDefaults.UpstreamPort}",
                    CommandOptionType.SingleValue);

                var proxyConfigFileOption = command.Option(
                    "-c|--configuration-file <path>",
                    "The path to a proxy configuration file in JSON format.",
                    CommandOptionType.SingleValue);

                command.OnExecute(async () =>
                {
                    if (!proxyPortOption.HasValue() || !int.TryParse(proxyPortOption.Value(), out int proxyPort))
                        proxyPort = Modes.FunctionalTest.OptionDefaults.ProxyPort;

                    if (!upstreamPortOption.HasValue() || !int.TryParse(upstreamPortOption.Value(), out int upstreamPort))
                        upstreamPort = Modes.FunctionalTest.OptionDefaults.UpstreamPort;

                    string proxyConfigFile = proxyConfigFileOption.HasValue()
                        ? proxyConfigFileOption.Value()
                        : Modes.FunctionalTest.OptionDefaults.ProxyConfigFile;

                    if (!File.Exists(proxyConfigFile))
                        throw new CommandParsingException(command, $"File '{proxyConfigFile}' cannot be found");

                    await Modes.FunctionalTest.Startup.StartAsync(proxyPort, upstreamPort, proxyConfigFile);
                    Console.ReadKey(false);
                    return 0;
                });
            });

            app.OnExecute(() => defaultCommand.Execute());
            return app;
        }

        private static void Execute(CommandLineApplication app, string[] args)
        {
            try
            {
                app.Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}