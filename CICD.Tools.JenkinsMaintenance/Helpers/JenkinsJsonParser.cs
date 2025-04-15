// ReSharper disable StringLiteralTypo
namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Helpers
{
    using System;
    using System.Linq;
    using System.Text.Json;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;

    internal class JenkinsJsonParser(ILogger<JenkinsJsonParser> logger)
    {
        public IJob? ParseJob(JsonElement element, JsonSerializerOptions? options = null)
        {
            if (!element.TryGetProperty("_class", out JsonElement value))
            {
                return null;
            }

            string? jobType = value.GetString()?.Split('.').Last();
            if (jobType == null)
            {
                return null;
            }

            switch (jobType.ToLowerInvariant())
            {
                case "workflowjob":
                case "workflowmultibranchproject":
                case "freestyleproject":
                case "matrixproject":
                    Workflow? workflow = element.Deserialize<Workflow>(options);
                    if (workflow == null)
                    {
                        logger.LogError("Failed to parse workflow '{job}'.", element);
                    }

                    return workflow;

                case "folder":
                case "organizationfolder":
                    Folder? folder = element.Deserialize<Folder>(options);
                    if (folder == null)
                    {
                        logger.LogError("Failed to parse folder '{job}'.", element);
                    }

                    return folder;

                default:
                    logger.LogDebug("Job class '{type}' is not supported. ({json})", jobType, element);
                    return null;
            }
        }

        public Node? ParseNode(string response, JsonSerializerOptions? options = null)
        {
            JsonDocument document = JsonDocument.Parse(response);
            return ParseNode(document.RootElement, options);
        }

        public Node? ParseNode(JsonElement element, JsonSerializerOptions? options = null)
        {
            Node? node = element.Deserialize<Node>(options);
            if (node == null)
            {
                logger.LogError("Failed to parse node '{node}'.", element);
                return null;
            }

            if (!element.TryGetProperty("_class", out JsonElement @class))
            {
                return null;
            }

            string className = @class.GetString() ?? String.Empty;
            node.UrlName = className.Contains("MasterComputer") ? "(built-in)" : node.DisplayName;

            return node;
        }
    }
}