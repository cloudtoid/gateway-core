namespace Cloudtoid.GatewayCore.Cli
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Extensions.CommandLineUtils;
    using Microsoft.Extensions.Configuration;

    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = SetupCommandLineApplication();
            return Execute(app, args);
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
                command.HelpOption("-?|-h|--help");

                var proxyPortOption = command.Option(
                    "-p|--proxy-port <port>",
                    $"The port that the proxy is listening on. The default port is {OptionDefaults.ProxyPort}",
                    CommandOptionType.SingleValue);

                var proxyConfigFileOption = command.Option(
                    "-c|--configuration-file <path>",
                    "The path to a gateway configuration file in JSON format.",
                    CommandOptionType.SingleValue);

                command.OnExecute(async () =>
                {
                    if (!proxyConfigFileOption.HasValue())
                        throw new CommandParsingException(command, "A gateway configuration file must be specified.");

                    if (!proxyPortOption.HasValue() || !int.TryParse(proxyPortOption.Value(), out int proxyPort))
                        proxyPort = OptionDefaults.ProxyPort;

                    Console.WriteLine($"Proxy: http://localhost:{proxyPort}/");
                    Console.WriteLine($"Gateway config: {proxyConfigFileOption.Value()}/");

                    var proxyConfig = LoadConfig(command, proxyConfigFileOption.Value());
                    await Startup.StartAsync(proxyPort, proxyConfig);

                    Console.WriteLine($"CLI is running.");
                    Console.ReadKey(true);
                    return 0;
                });
            });

            app.OnExecute(() => defaultCommand.Execute());
            return app;
        }

        private static int Execute(CommandLineApplication app, string[] args)
        {
            try
            {
                app.Execute(args);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        private static IConfiguration LoadConfig(CommandLineApplication command, string configFile)
        {
            var file = new FileInfo(configFile);
            if (!file.Exists)
                throw new CommandParsingException(command, $"File '{configFile}' cannot be found.");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(file.Directory.FullName)
                .AddJsonFile(file.Name, optional: false, reloadOnChange: false);

            return configBuilder.Build();
        }
    }
}