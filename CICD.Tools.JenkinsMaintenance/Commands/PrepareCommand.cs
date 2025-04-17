namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.Collections.Generic;
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

    internal class PrepareCommand : BaseCommand
    {
        public PrepareCommand() :
            base(name: "prepare", description: "Prepare Jenkins for upcoming maintenance by disabling all nodes.")
        {
            AddOption(new Option<IFileInfoIO>(
                aliases: ["--maintenance-file", "-mf"],
                description: "The file that will be created that holds information to be used for the resume command.",
                parseArgument: OptionHelper.ParseFileInfo!)
            {
                IsRequired = true
            }.LegalFilePathsOnly());

            AddOption(new Option<bool?>(
                aliases: ["--safe", "-s"],
                description: "Safety option to test the command without changing things on Jenkins."));
        }
    }

    internal class PrepareCommandHandler(ILogger<PrepareCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
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
            logger.LogDebug($"Starting {nameof(PrepareCommand)}...");

            if (Safe is true)
            {
                logger.LogInformation("-=- SAFE MODE -=-");
            }

            try
            {
                // Create file first to make sure that it can be created
                MaintenanceFile.Directory.Create();
                await MaintenanceFile.Create().DisposeAsync();

                jenkins.SetUriAndCredentials(Uri, JenkinsUserId, Token);

                MaintenanceInfo info = new MaintenanceInfo();

                IEnumerable<Node> nodes = await jenkins.GetNodesAsync();
                foreach (Node node in nodes)
                {
                    if (node.IsOffline == true)
                    {
                        logger.LogInformation("Node '{nodeName}' is already offline.", node.DisplayName);
                        continue;
                    }

                    if (Safe is not true)
                    {
                        bool result = await jenkins.ToggleNodeAsync(node.UrlName!);
                        logger.LogInformation("Disabled node '{name}': {result}", node.DisplayName, result);
                    }
                    else
                    {
                        logger.LogInformation("Disabled node '{name}'", node.DisplayName);
                    }

                    info.DisabledNodes.Add(node.UrlName!);
                }

                // Store maintenance information in the file
                await using FileStream fileStream = MaintenanceFile.OpenWrite();
                await using StreamWriter streamWriter = new StreamWriter(fileStream);
                await streamWriter.WriteAsync(JsonSerializer.Serialize(info));

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to disable all workflows.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(PrepareCommand)}.");
            }
        }
    }
}