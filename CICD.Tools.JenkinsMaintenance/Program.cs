namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance
{
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Hosting;
    using System.CommandLine.Parsing;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Serilog;
    using Serilog.Events;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands.Admin;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Helpers;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;

    /// <summary>
    /// This .NET tool allows you to manage Jenkins for maintenance windows.
    /// </summary>
    public static class Program
    {
        /*
         * Design guidelines for command line tools: https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#design-guidance
         */

        /// <summary>
        /// Code that will be called when running the tool.
        /// </summary>
        /// <param name="args">Extra arguments.</param>
        /// <returns>0 if successful.</returns>
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("This .NET tool allows you to manage Jenkins for maintenance windows.")
            {
                new PrepareCommand(),
                new ResumeCommand(),
                new InfoCommand(),
                new QuietDownCommand(),
                new CancelQuietDownCommand(),
                new CheckEmptyCommand(),

                // Admin commands (hidden)
                new KillCommand(),
                new ResurrectCommand(),
                new GreaterResurrectCommand(),
            };

            var isDebug = new Option<bool>(
                name: "--debug",
                description: "Indicates the tool should write out debug logging.")
            {
                IsHidden = true
            };

            var logLevel = new Option<LogEventLevel>(
                name: "--minimum-log-level",
                description: "Indicates what the minimum log level should be. Default is Information", getDefaultValue: () => LogEventLevel.Information);

            rootCommand.AddGlobalOption(isDebug);
            rootCommand.AddGlobalOption(logLevel);

            ParseResult parseResult = rootCommand.Parse(args);
            LogEventLevel level = parseResult.GetValueForOption(isDebug)
                ? LogEventLevel.Debug
                : parseResult.GetValueForOption(logLevel);

            var builder = new CommandLineBuilder(rootCommand).UseDefaults().UseHost(host =>
            {
                host.ConfigureServices(services =>
                    {
                        services.AddLogging(loggingBuilder =>
                                {
                                    loggingBuilder.AddSerilog(
                                        new LoggerConfiguration()
                                            .MinimumLevel.Is(level)
                                            .WriteTo.Console()
                                            .CreateLogger());
                                })
                                .AddSingleton<JenkinsJsonParser>()
                                .AddHttpClient<IJenkinsService, JenkinsService>();
                    })
                    .ConfigureHostConfiguration(configurationBuilder =>
                    {
                        configurationBuilder.AddUserSecrets<IJenkinsService>() // For easy testing
                                            .AddEnvironmentVariables();
                    })
                    .UseCommandHandler<PrepareCommand, PrepareCommandHandler>()
                    .UseCommandHandler<ResumeCommand, ResumeCommandHandler>()
                    .UseCommandHandler<InfoCommand, InfoCommandHandler>()
                    .UseCommandHandler<QuietDownCommand, QuietDownCommandHandler>()
                    .UseCommandHandler<CancelQuietDownCommand, CancelQuietDownCommandHandler>()
                    .UseCommandHandler<CheckEmptyCommand, CheckEmptyCommandHandler>()
                    .UseCommandHandler<KillCommand, KillCommandHandler>()
                    .UseCommandHandler<ResurrectCommand, ResurrectCommandHandler>()
                    .UseCommandHandler<GreaterResurrectCommand, GreaterResurrectCommandHandler>();
            });

            return await builder.Build().InvokeAsync(args);
        }
    }
}