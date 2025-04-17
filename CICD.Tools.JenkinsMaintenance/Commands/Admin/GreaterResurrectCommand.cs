namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands.Admin
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;

    internal class GreaterResurrectCommand : BaseCommand
    {
        public GreaterResurrectCommand() :
            base(name: "greater-resurrect", description: "!! WARNING: Don't use without proper knowledge !! This will enable ALL nodes and workflows. Regardless if they were disabled before the kill command.")
        {
            IsHidden = true; // Admin command that should not be shown to users

            AddOption(new Option<bool?>(
                aliases: ["--safe", "-s"],
                description: "Safety option to test the command without changing things on Jenkins.",
                getDefaultValue: () => true));
        }
    }

    internal class GreaterResurrectCommandHandler(ILogger<GreaterResurrectCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required bool Safe { get; set; }

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(GreaterResurrectCommand)}...");

            if (Safe)
            {
                logger.LogInformation("-=- SAFE MODE -=-");
            }

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                jenkins.SetUriAndCredentials(Uri, JenkinsUserId, Token);

                var workflows = await jenkins.GetWorkflowsAsync();
                foreach (var workflow in workflows)
                {
                    if (!Safe)
                    {
                        bool result = await jenkins.EnableJobAsync(workflow.Url);
                        logger.LogInformation("Enabled job '{url}': {result}", workflow.Url, result);
                    }
                    else
                    {
                        logger.LogInformation("Enabled job '{url}'", workflow.Url);
                    }
                }

                IEnumerable<Node> nodes = await jenkins.GetNodesAsync();
                foreach (var node in nodes)
                {
                    if (node.IsOffline is not true)
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
                logger.LogDebug($"Finished {nameof(GreaterResurrectCommand)} in {sw.Elapsed}.");
            }
        }
    }
}