namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.CommandLine.Invocation;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;

    internal class CancelQuietDownCommand() : BaseCommand(name: "cancel-quietdown", description: "Cancel the shutdown of Jenkins.");

    internal class CancelQuietDownCommandHandler(ILogger<CancelQuietDownCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(CancelQuietDownCommand)}...");

            try
            {
                jenkins.SetUriAndCredentials(Uri, Username, Token);

                await jenkins.CancelQuietDownAsync();

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to cancel the shutdown of Jenkins.");
                return (int)ExitCodes.UnexpectedException;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(CancelQuietDownCommand)}.");
            }
        }
    }
}