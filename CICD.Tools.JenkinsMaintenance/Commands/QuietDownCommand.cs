namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.CommandLine.Invocation;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;

    internal class QuietDownCommand() : BaseCommand(name: "quiet-down",
        description: "Prepare Jenkins for a shutdown. It will stop any new builds and pause ongoing builds.");

    internal class QuietDownCommandHandler(ILogger<QuietDownCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(QuietDownCommand)}...");

            try
            {
                jenkins.SetUriAndCredentials(Uri, JenkinsUserId, Token);

                await jenkins.QuietDownAsync();

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to quiet down Jenkins.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(QuietDownCommand)}.");
            }
        }
    }
}