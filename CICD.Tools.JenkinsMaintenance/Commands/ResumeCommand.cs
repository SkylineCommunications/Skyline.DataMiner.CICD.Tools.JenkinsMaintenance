namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

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

            AddOption(new Option<UInt32?>(
            aliases: ["--wait-until-up", "-w"],
            description: "Will retry until jenkins service is available or provided timeout time in minutes. Waits infinite when providing 0. Never waits when left empty."));

            AddOption(new Option<bool?>(
                aliases: ["--safe", "-s"],
                description: "Safety option to test the command without changing things on Jenkins."));
        }
    }

    internal class ResumeCommandHandler(ILogger<ResumeCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required IFileInfoIO MaintenanceFile { get; set; }

        public UInt32? WaitUntilUp { get; set; }

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
                jenkins.SetUriAndCredentials(Uri, JenkinsUserId, Token);

                if (WaitUntilUp != null)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    UInt32 waitInMs = (UInt32)WaitUntilUp * 60 * 1000;
                    bool isUp = false;
                    while (!isUp && (WaitUntilUp == 0 || sw.ElapsedMilliseconds <= waitInMs))
                    {
                        logger.LogInformation("Checking if Jenkins is up...");
                        try
                        {
                            if (await jenkins.GetBuildQueueAsync() != null) isUp = true;
                        }
                        catch
                        {
                            isUp = false;
                        }

                        logger.LogInformation("Jenkins status: {isUp}", isUp ? "Up" : "Down");

                        // Always wait a minute to allow all jenkins nodes to become ready for API calls even when UP.
                        Thread.Sleep(60000);
                    }

                    sw.Stop();
                }

                MaintenanceInfo info;

                // Read the file that was created by the prepare command
                using (StreamReader streamReader = MaintenanceFile.OpenText())
                {
                    string json = await streamReader.ReadToEndAsync();

                    if (String.IsNullOrEmpty(json))
                    {
                        logger.LogError("Maintenance File was empty, unable to resume.");
                        return (int)ExitCodes.GeneralError;
                    }

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