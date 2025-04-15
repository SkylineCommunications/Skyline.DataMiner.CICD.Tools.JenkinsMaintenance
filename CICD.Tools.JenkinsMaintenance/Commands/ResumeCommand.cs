namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Models;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.SystemCommandLine;

    internal class ResumeCommand : BaseCommand
    {
        public ResumeCommand() :
            base(name: "resume", description: "Resume activity on Jenkins by enabling all the nodes that have been disabled during the 'prepare' command.")
        {
            AddOption(new Option<IFileInfoIO>(
                aliases: ["--maintenance-file", "-mf"],
                description: "The file that was created by the prepare command.",
                parseArgument: OptionHelper.ParseFileInfo!)
            {
                IsRequired = true
            }.LegalFilePathsOnly()!.ExistingOnly());

            AddOption(new Option<bool?>(
                aliases: ["--safe", "-s"],
                description: "Safety option to test the command without changing things on Jenkins."));
        }
    }

    internal class ResumeCommandHandler(ILogger<ResumeCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required IFileInfoIO MaintenanceFile { get; set; }

        public bool? Safe { get; set; }

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(ResumeCommand)}...");

            if (Safe is true)
            {
                logger.LogInformation("-=- SAFE MODE -=-");
            }

            try
            {
                jenkins.SetUriAndCredentials(Uri, Username, Token);

                MaintenanceInfo info;

                // Read the file that was created by the prepare command
                using (StreamReader streamReader = MaintenanceFile.OpenText())
                {
                    string json = await streamReader.ReadToEndAsync();
                    info = JsonSerializer.Deserialize<MaintenanceInfo>(json) ?? new MaintenanceInfo();
                }

                foreach (string nodeUrlName in info.DisabledNodes)
                {
                    Node? node = await jenkins.GetNodeAsync(nodeUrlName);
                    if (node == null)
                    {
                        logger.LogWarning("Node '{urlName}' is missing on Jenkins.", nodeUrlName);
                        continue;
                    }

                    if (node.IsOffline is not true)
                    {
                        logger.LogInformation("Node '{nodeName}' is already online.", node.DisplayName);
                        continue;
                    }

                    if (Safe is not true)
                    {
                        bool result = await jenkins.ToggleNodeAsync(nodeUrlName);
                        logger.LogInformation("Enabled Node '{name}': {result}", node.DisplayName, result);
                    }
                    else
                    {
                        logger.LogInformation("Enabled Node '{name}'", node.DisplayName);
                    }
                }

                if (Safe is not true)
                {
                    // Clean up maintenance file so that it doesn't try to enable already enabled nodes if command is called by accident
                    MaintenanceFile.Delete();
                }

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to enable all workflows.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(ResumeCommand)}.");
            }
        }
    }
}