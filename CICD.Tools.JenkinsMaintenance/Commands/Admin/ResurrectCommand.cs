namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands.Admin
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Models;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.SystemCommandLine;

    internal class ResurrectCommand : BaseCommand
    {
        public ResurrectCommand() :
            base(name: "resurrect", description: "!! WARNING: Don't use without proper knowledge !! An emergency command to resurrect Jenkins after a kill command.")
        {
            IsHidden = true; // Admin command that should not be shown to users

            AddOption(new Option<IFileInfoIO>(
                aliases: ["--maintenance-file", "-mf"],
                description: "The file that will be created that holds information on what was disabled or stopped.",
                parseArgument: OptionHelper.ParseFileInfo!)
            {
                IsRequired = true
            }.LegalFilePathsOnly());

            AddOption(new Option<bool?>(
                aliases: ["--safe", "-s"],
                description: "Safety option to test the command without changing things on Jenkins.",
                getDefaultValue: () => true));
        }
    }

    internal class ResurrectCommandHandler(ILogger<ResurrectCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required IFileInfoIO MaintenanceFile { get; set; }

        public required bool Safe { get; set; }

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(ResurrectCommand)}...");

            if (Safe)
            {
                logger.LogInformation("-=- SAFE MODE -=-");
            }

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                jenkins.SetUriAndCredentials(Uri, Username, Token);

                KillInfo info;

                // Read the file that was created by the prepare command
                using (StreamReader streamReader = MaintenanceFile.OpenText())
                {
                    string json = await streamReader.ReadToEndAsync();
                    info = JsonSerializer.Deserialize<KillInfo>(json) ?? new KillInfo();
                }

                foreach (string workflowUrl in info.DisabledWorkflows)
                {
                    if (!Safe)
                    {
                        bool result = await jenkins.EnableJobAsync(workflowUrl);
                        logger.LogInformation("Enabled job '{url}': {result}", workflowUrl, result);
                    }
                    else
                    {
                        logger.LogInformation("Enabled job '{url}'", workflowUrl);
                    }
                }

                foreach (string nodeName in info.DisabledNodes)
                {
                    Node? node = await jenkins.GetNodeAsync(nodeName);
                    if (node?.IsOffline is not true)
                    {
                        continue;
                    }

                    if (!Safe)
                    {
                        bool result = await jenkins.ToggleNodeAsync(node.UrlName!);
                        logger.LogInformation("Enabled node '{name}': {result}", node.DisplayName, result);
                    }
                    else
                    {
                        logger.LogInformation("Enabled node '{name}'", node.DisplayName);
                    }
                }

                if (!Safe)
                {
                    await jenkins.CancelQuietDownAsync();

                    // Clean up maintenance file so that it doesn't try to enable already enabled jobs if command is called by accident
                    MaintenanceFile.Delete();
                }

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to resurrect everything in Jenkins.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                sw.Stop();
                logger.LogDebug($"Finished {nameof(ResurrectCommand)} in {sw.Elapsed}.");
            }
        }
    }
}