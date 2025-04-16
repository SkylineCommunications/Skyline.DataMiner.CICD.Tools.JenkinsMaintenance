namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
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
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Models;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.SystemCommandLine;

    internal class InfoCommand : BaseCommand
    {
        public InfoCommand() :
            base(name: "info", description: "Show a list of all the nodes and pipelines.")
        {
            AddOption(new Option<IFileInfoIO?>(
                aliases: ["--info-file", "-if"],
                description: "The file that will be created that holds the information.",
                parseArgument: OptionHelper.ParseFileInfo)
            {
                IsRequired = false
            }.LegalFilePathsOnly());
        }
    }

    internal class InfoCommandHandler(ILogger<InfoCommandHandler> logger, IJenkinsService jenkins) : BaseCommandHandler
    {
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true
        };

        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public IFileInfoIO? InfoFile { get; set; }

        public override int Invoke(InvocationContext context)
        {
            return (int)ExitCodes.NotImplemented;
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"Starting {nameof(InfoCommand)}...");
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                if (InfoFile != null)
                {
                    // Create file first to make sure that it can be created
                    InfoFile.Directory.Create();
                    await InfoFile.Create().DisposeAsync();
                }

                jenkins.SetUriAndCredentials(Uri, Username, Token);

                IEnumerable<Workflow> workflows = await jenkins.GetWorkflowsAsync();
                IEnumerable<Node> nodes = await jenkins.GetNodesAsync();

                GeneralInfo info = new GeneralInfo
                {
                    Nodes = nodes.ToList(),
                    WorkFlows = workflows.ToList()
                };

                string result = JsonSerializer.Serialize(info, jsonSerializerOptions);

                if (InfoFile != null)
                {
                    await using FileStream fileStream = InfoFile.OpenWrite();
                    await using StreamWriter streamWriter = new StreamWriter(fileStream);
                    await streamWriter.WriteAsync(result);
                }

                logger.LogInformation(result);
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
                logger.LogDebug($"Finished {nameof(InfoCommand)} in {sw.Elapsed}.");
            }
        }
    }
}