namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.JenkinsModels;

    internal interface IJenkinsService
    {
        /// <summary>
        /// Set the Jenkins URI and credentials.
        /// </summary>
        /// <param name="jenkinsUri">The jenkins uri.</param>
        /// <param name="username">The username.</param>
        /// <param name="token">The token for the specified username.</param>
        void SetUriAndCredentials(Uri? jenkinsUri = null, string? username = null, string? token = null);

        /// <summary>
        /// Quiet down Jenkins.
        /// </summary>
        Task QuietDownAsync();

        /// <summary>
        /// Cancel the quiet down of Jenkins.
        /// </summary>
        Task CancelQuietDownAsync();

        /// <summary>
        /// Disable a job in Jenkins.
        /// </summary>
        /// <param name="url">The job url.</param>
        /// <returns><c>true</c> if the job was disabled; otherwise, <c>false</c>.</returns>
        Task<bool> DisableJobAsync(string url);

        /// <summary>
        /// Enable a job in Jenkins.
        /// </summary>
        /// <param name="url">The job url.</param>
        /// <returns><c>true</c> if the job was enabled; otherwise, <c>false</c>.</returns>
        Task<bool> EnableJobAsync(string url);

        /// <summary>
        /// Get a node by name.
        /// </summary>
        /// <param name="name">The node name.</param>
        /// <returns>A <see cref="Node"/> object if the node is found; otherwise, <c>null</c>.</returns>
        Task<Node?> GetNodeAsync(string name);

        /// <summary>
        /// Get all nodes in Jenkins.
        /// </summary>
        /// <returns>A list of <see cref="Node"/> objects.</returns>
        Task<IEnumerable<Node>> GetNodesAsync();

        /// <summary>
        /// Get all workflows in Jenkins.
        /// </summary>
        /// <returns>A list of <see cref="Workflow"/> objects.</returns>
        Task<IEnumerable<Workflow>> GetWorkflowsAsync();

        /// <summary>
        /// Get a job by URL.
        /// </summary>
        /// <param name="url">The job url.</param>
        /// <returns>A <see cref="IJob"/> object if the job is found; otherwise, <c>null</c>.</returns>
        Task<IJob?> GetJobByUrlAsync(string url);

        /// <summary>
        /// Get the build queue of Jenkins.
        /// </summary>
        /// <returns>A <see cref="BuildQueue"/> object if the build queue is found; otherwise, <c>null</c>.</returns>
        Task<BuildQueue?> GetBuildQueueAsync();

        /// <summary>
        /// Cancel the specified queue item.
        /// </summary>
        /// <param name="id">The queue item id.</param>
        /// <returns><c>true</c> if the item was cancelled; otherwise, <c>false</c>.</returns>
        Task<bool> CancelQueueItemAsync(long id);

        /// <summary>
        /// Kill the specified job build.
        /// </summary>
        /// <param name="url">The job url.</param>
        /// <param name="number">The build number.</param>
        /// <returns><c>true</c> if the job was killed; otherwise, <c>false</c>.</returns>
        Task<bool> KillJobBuildAsync(string url, int number);

        /// <summary>
        /// Stops the specified job build.
        /// </summary>
        /// <param name="url">The job url.</param>
        /// <param name="number">The build number.</param>
        /// <returns><c>true</c> if the job was stopped; otherwise, <c>false</c>.</returns>
        Task<bool> StopJobBuildAsync(string url, int number);

        /// <summary>
        /// Toggles the status of the specified node.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns><c>true</c> if the node status was successfully toggled; otherwise, <c>false</c>.</returns>
        Task<bool> ToggleNodeAsync(string nodeName);
    }
}