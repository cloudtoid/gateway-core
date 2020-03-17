namespace Cloudtoid.Foid.Cli
{
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
                Description = "Cloudtoid FOID Reverse Proxy CLI"
            };
            app.HelpOption("-?|-h|--help");

            var version = StringUtil.FormatInvariant("Version {0}", assembly.GetName().Version);
            app.VersionOption("-v|--version", version);

            var defaultCommand = app.Command("default", (command) =>
            {
                command.Description = "Run the CLI in its default mode.";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(() => Modes.FunctionalTest.Startup.Run());
            });

            app.Command("functional-test", (command) =>
             {
                 command.Description = "Run in the functional-test mode.";
                 command.ExtendedHelpText = "This will run a specific proxy server that is used by the functional tests.";
                 command.HelpOption("-?|-h|--help");

                 command.OnExecute(() => Modes.FunctionalTest.Startup.Run());
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
            catch (CommandParsingException e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
    }
}