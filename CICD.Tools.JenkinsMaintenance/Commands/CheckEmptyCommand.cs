namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Invocation;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;

    internal class CheckEmptyCommand() : BaseCommand(name: "check-empty",
        description: "Checks if the build queue is empty and nothing is currently running. Will fail when something is still running.");

    internal class CheckEmptyCommandHandler(ILogger<CheckEmptyCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(CheckEmptyCommand)}...");
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                jenkins.SetUriAndCredentials(Uri, Username, Token);

                BuildQueue? buildQueueAsync = await jenkins.GetBuildQueueAsync();
                if (buildQueueAsync?.Items.Count != 0)
                {
                    logger.LogError("The build queue is not empty.");
                    return (int)ExitCodes.GeneralError;
                }

                IEnumerable<Node> nodes = await jenkins.GetNodesAsync();
                if (nodes.Any(node => node.Idle != true))
                {
                    logger.LogError("Not all nodes are idle.");
                    return (int)ExitCodes.GeneralError;
                }

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to receive all info.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                sw.Stop();
                logger.LogDebug($"Finished {nameof(CheckEmptyCommand)} in {sw.Elapsed}.");
            }
        }
    }
}