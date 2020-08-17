using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace Cloudtoid.GatewayCore.Server
{
    public static class Program
    {
        private const string DefaultConfigFile = "default-config.json";

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
                Description = "Cloudtoid Gateway Core Server"
            };
            app.HelpOption("-?|-h|--help");

            var configFileOption = app.Option(
                "-c|--configuration-file <path>",
                $"The path to a configuration file in JSON format. The default is '{DefaultConfigFile}' that is included.",
                CommandOptionType.SingleValue);

            var version = StringUtil.FormatInvariant("Version {0}", assembly.GetName().Version);
            app.VersionOption("-v|--version", version);

            var defaultCommand = app.Command("default", (command) =>
            {
                command.OnExecute(async () =>
                {
                    var configFile = configFileOption.HasValue()
                        ? configFileOption.Value()
                        : DefaultConfigFile;

                    Console.WriteLine($"Configuration is loaded from '{configFile}'");

                    var config = LoadConfig(command, configFile);
                    await Startup.StartAsync(config);

                    Console.WriteLine($"Gateway Core is running.");
                    Console.Read();
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