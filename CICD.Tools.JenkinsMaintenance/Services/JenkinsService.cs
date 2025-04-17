namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Helpers;
    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;

    internal class JenkinsService(HttpClient httpClient, ILogger<JenkinsService> logger, IConfiguration configuration, JenkinsJsonParser jsonParser) : IJenkinsService
    {
        private bool setupComplete;

        public void SetUriAndCredentials(Uri? jenkinsUri = null, string? userId = null, string? token = null)
        {
            if (jenkinsUri == null)
            {
                string? uriString = configuration["jenkins:uri"];
                if (String.IsNullOrWhiteSpace(uriString))
                {
                    throw new InvalidOperationException("No jenkins URI provided.");
                }

                if (!uriString.EndsWith('/'))
                {
                    uriString += '/';
                }

                jenkinsUri = new Uri(uriString);
            }

            userId ??= configuration["jenkins:userid"];
            token ??= configuration["jenkins:token"];

            if (String.IsNullOrWhiteSpace(userId) || String.IsNullOrWhiteSpace(token))
            {
                throw new InvalidCredentialException("No userid or token provided.");
            }

            httpClient.BaseAddress = jenkinsUri;

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userId}:{token}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            setupComplete = true;
        }

        public async Task QuietDownAsync()
        {
            logger.LogDebug($"Starting {nameof(QuietDownAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.PostAsync("quietDown", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to put Jenkins in quiet mode. [{statusCode}] {reason}", response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return;
                }

                logger.LogInformation("Jenkins is now in quiet mode.");
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(QuietDownAsync)}.");
            }
        }

        public async Task CancelQuietDownAsync()
        {
            logger.LogDebug($"Starting {nameof(CancelQuietDownAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.PostAsync("cancelQuietDown", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to cancel quiet mode. [{statusCode}] {reason}", response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return;
                }

                logger.LogInformation("Jenkins is now out of quiet mode.");
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(CancelQuietDownAsync)}.");
            }
        }

        public async Task<bool> DisableJobAsync(string url)
        {
            logger.LogDebug($"Starting {nameof(DisableJobAsync)}...");
            
            try
            {
                EnsureSetup();

                if (!url.EndsWith('/'))
                {
                    url += '/';
                }

                var response = await httpClient.PostAsync($"{url}disable", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to disable job '{url}'. [{statusCode}] {reason}", url, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return false;
                }

                return true;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(DisableJobAsync)}.");
            }
        }

        public async Task<bool> EnableJobAsync(string url)
        {
            logger.LogDebug($"Starting {nameof(EnableJobAsync)}...");

            try
            {
                EnsureSetup();

                if (!url.EndsWith('/'))
                {
                    url += '/';
                }

                var response = await httpClient.PostAsync($"{url}enable", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to enable job '{url}'. [{statusCode}] {reason}", url, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return false;
                }

                return true;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(EnableJobAsync)}.");
            }
        }

        public async Task<Node?> GetNodeAsync(string name)
        {
            logger.LogDebug($"Starting {nameof(GetNodeAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.GetAsync($"computer/{HttpUtility.UrlEncode(name)}/api/json?tree={Node.Tree}");
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to get node information for '{nodeName}'. [{statusCode}] {reason}", name, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return null;
                }

                return jsonParser.ParseNode(respContent);
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(GetNodeAsync)}.");
            }
        }

        public async Task<IEnumerable<Node>> GetNodesAsync()
        {
            logger.LogDebug($"Starting {nameof(GetNodesAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.GetAsync($"computer/api/json?tree=computer[{Node.Tree}]");
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to get nodes. [{statusCode}] {reason}", response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return [];
                }

                JsonDocument doc = JsonDocument.Parse(respContent);

                if (!doc.RootElement.TryGetProperty("computer", out JsonElement value))
                {
                    return [];
                }

                return value.EnumerateArray().Select(computer => jsonParser.ParseNode(computer)).OfType<Node>().ToList();
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(GetNodesAsync)}.");
            }
        }

        public async Task<IEnumerable<Workflow>> GetWorkflowsAsync()
        {
            logger.LogDebug($"Starting {nameof(GetWorkflowsAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.GetAsync($"api/json?tree=jobs[{Job.Tree},{Workflow.Tree},{Folder.Tree},_class]");
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to get workflows. [{statusCode}] {reason}", response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return [];
                }

                JsonDocument doc = JsonDocument.Parse(respContent);
                if (!doc.RootElement.TryGetProperty("jobs", out JsonElement jobs))
                {
                    return [];
                }

                List<Workflow> workflows = [];
                Queue<string> urlsToPoll = new Queue<string>();
                foreach (JsonElement element in jobs.EnumerateArray())
                {
                    IJob? job = jsonParser.ParseJob(element);
                    HandleJob(job, workflows, urlsToPoll);
                }

                while (urlsToPoll.Count > 0)
                {
                    string url = urlsToPoll.Dequeue();

                    IJob? job = await GetJobByUrlAsync(url);
                    HandleJob(job, workflows, urlsToPoll);
                }

                return workflows;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(GetWorkflowsAsync)}.");
            }

            static void HandleJob(IJob? job, List<Workflow> workflows, Queue<string> urlsToPoll)
            {
                if (job == null)
                {
                    return;
                }

                switch (job)
                {
                    case Workflow workflow:
                        workflows.Add(workflow);
                        break;
                    case Folder folder:
                        {
                            foreach (SubJob subJob in folder.Jobs ?? [])
                            {
                                if (subJob.Url != null)
                                {
                                    urlsToPoll.Enqueue(subJob.Url);
                                }
                            }

                            break;
                        }
                }
            }
        }

        public async Task<IJob?> GetJobByUrlAsync(string url)
        {
            logger.LogDebug($"Starting {nameof(GetJobByUrlAsync)}...");

            try
            {
                EnsureSetup();

                if (!url.EndsWith('/'))
                {
                    url += '/';
                }

                var response = await httpClient.GetAsync($"{url}api/json?tree={Job.Tree},{Workflow.Tree},{Folder.Tree},_class");
                string respContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to get job by url '{url}'. [{statusCode}] {reason}", url, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return null;
                }

                JsonDocument doc = JsonDocument.Parse(respContent);
                return jsonParser.ParseJob(doc.RootElement);
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(GetJobByUrlAsync)}.");
            }
        }

        public async Task<BuildQueue?> GetBuildQueueAsync()
        {
            logger.LogDebug($"Starting {nameof(GetBuildQueueAsync)}");

            try
            {
                EnsureSetup();

                var response = await httpClient.GetAsync($"queue/api/json?tree={BuildQueue.Tree}");
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to get the build queue. [{statusCode}] {reason}", response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return null;
                }

                return JsonSerializer.Deserialize<BuildQueue>(respContent);
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(GetBuildQueueAsync)}.");
            }
        }

        public async Task<bool> CancelQueueItemAsync(long id)
        {
            logger.LogDebug($"Starting {nameof(CancelQueueItemAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.PostAsync($"queue/cancelItem?id={id}", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to cancel the queue item with id '{id}'. [{statusCode}] {reason}", id, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return false;
                }

                return true;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(CancelQueueItemAsync)}.");
            }
        }

        public async Task<bool> KillJobBuildAsync(string url, int number)
        {
            logger.LogDebug($"Starting {nameof(KillJobBuildAsync)}...");

            try
            {
                EnsureSetup();

                if (!url.EndsWith('/'))
                {
                    url += '/';
                }

                var response = await httpClient.PostAsync($"{url}{number}/kill", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to kill the build '{number}' of job '{job}'. [{statusCode}] {reason}", number, url, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return false;
                }

                return true;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(KillJobBuildAsync)}.");
            }
        }

        public async Task<bool> StopJobBuildAsync(string url, int number)
        {
            logger.LogDebug($"Starting {nameof(StopJobBuildAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.PostAsync($"{url}{number}/stop", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to stop the build '{number}' of job '{job}'. [{statusCode}] {reason}", number, url, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return false;
                }

                return true;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(StopJobBuildAsync)}.");
            }
        }

        public async Task<bool> ToggleNodeAsync(string nodeName)
        {
            logger.LogDebug($"Starting {nameof(ToggleNodeAsync)}...");

            try
            {
                EnsureSetup();

                var response = await httpClient.PostAsync($"computer/{HttpUtility.UrlEncode(nodeName)}/toggleOffline", null);
                string respContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to toggle the node '{nodeName}'. [{statusCode}] {reason}", nodeName, response.StatusCode, response.ReasonPhrase);
                    logger.LogDebug("Full response: {respContent}", respContent);
                    return false;
                }

                return true;
            }
            finally
            {
                logger.LogDebug($"Finished {nameof(ToggleNodeAsync)}.");
            }
        }

        private void EnsureSetup()
        {
            if (setupComplete)
            {
                return;
            }

            // Try on environment variables only
            SetUriAndCredentials();

            if (setupComplete)
            {
                return;
            }

            // Shouldn't happen normally as the SetUriAndCredentials method will throw an exception if something is missing.
            throw new InvalidOperationException($"JenkinsService is not set up. Call {nameof(SetUriAndCredentials)} first.");
        }
    }
}