namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands.Admin
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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

    internal class KillCommand : BaseCommand
    {
        public KillCommand() :
            base(name: "kill", description: "!! WARNING: Don't use without proper knowledge !! An emergency command to disable every workflow, node. It will clear the build queue and forcefully stop running workflows.")
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

    internal class KillCommandHandler(ILogger<KillCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
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
            logger.LogDebug($"Starting {nameof(KillCommand)}...");

            if (Safe)
            {
                logger.LogInformation("-=- SAFE MODE -=-");
            }

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                // Create file first to make sure that it can be created
                MaintenanceFile.Directory.Create();
                await MaintenanceFile.Create().DisposeAsync();

                jenkins.SetUriAndCredentials(Uri, Username, Token);

                IEnumerable<Workflow> workflows = (await jenkins.GetWorkflowsAsync()).ToList();
                KillInfo info = new KillInfo();
                foreach (Workflow workflow in workflows)
                {
                    if (workflow.IsEnabled != true)
                    {
                        // Don't try to disable an already disabled job or jobs that can't be disabled (will give error)
                        logger.LogInformation("Job '{jobName}' is already disabled.", workflow.DisplayName);
                        continue;
                    }

                    if (Safe is not true)
                    {
                        bool result = await jenkins.DisableJobAsync(workflow.Url);
                        logger.LogInformation("Disabled job '{name}': {result}", workflow.Name, result);
                    }
                    else
                    {
                        logger.LogInformation("Disabled job '{name}'", workflow.Name);
                    }

                    info.DisabledWorkflows.Add(workflow.Url);
                }

                // Disable all nodes
                IEnumerable<Node> nodes = await jenkins.GetNodesAsync();
                foreach (Node node in nodes)
                {
                    if (node.IsOffline == true)
                    {
                        // If node is offline, don't toggle as it would put it online again
                        logger.LogInformation("Node '{nodeName}' is already disabled.", node.DisplayName);
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

                    info.DisabledNodes.Add(node.DisplayName);
                }

                // Clear the build queue
                BuildQueue? buildQueueAsync = await jenkins.GetBuildQueueAsync();
                if (buildQueueAsync?.Items is { Count: > 0 })
                {
                    foreach (QueueItem queueItem in buildQueueAsync.Items)
                    {
                        if (Safe is not true)
                        {
                            bool result = await jenkins.CancelQueueItemAsync(queueItem.Id);
                            logger.LogInformation("Cancelled queue item '{name}': {result}", queueItem.Id, result);
                        }
                        else
                        {
                            logger.LogInformation("Cancelled queue item '{name}'", queueItem.Id);
                        }
                    }
                }

                // Kill any running jobs
                foreach (Workflow workflow in workflows)
                {
                    if (workflow.Builds == null)
                    {
                        continue;
                    }

                    foreach (var run in workflow.Builds.Where(build => build.Building == true))
                    {
                        if (Safe is not true)
                        {
                            bool result = await jenkins.KillJobBuildAsync(workflow.Url, run.Number);
                            logger.LogInformation("Killed job '{name}': {result}", workflow.Name, result);
                        }
                        else
                        {
                            logger.LogInformation("Killed job '{name}'", workflow.Name);
                        }
                    }
                }

                // Quiet down Jenkins
                if (Safe is not true)
                {
                    await jenkins.QuietDownAsync();
                }

                // Store maintenance information in the file
                await using FileStream fileStream = MaintenanceFile.OpenWrite();
                await using StreamWriter streamWriter = new StreamWriter(fileStream);
                await streamWriter.WriteAsync(JsonSerializer.Serialize(info));

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to kill everything in Jenkins.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                sw.Stop();
                logger.LogDebug($"Finished {nameof(KillCommand)} in {sw.Elapsed}.");
            }
        }
    }
}