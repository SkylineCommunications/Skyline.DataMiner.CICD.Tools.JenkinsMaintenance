namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Commands
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Threading.Tasks;

    internal abstract class BaseCommand : Command
    {
        protected BaseCommand(string name, string? description = null) : base(name, description)
        {
            AddOption(new Option<Uri?>(
                aliases: ["--uri", "-uri"],
                description: "The uri of the Jenkins to connect to. Can also be provided via an environment variable: 'Jenkins__Url'"));
            AddOption(new Option<string?>(
                aliases: ["--username", "-u"],
                description: "The 'Jenkins User ID' of the user to connect to Jenkins. Can also be provided via an environment variable: 'Jenkins__Username'"));
            AddOption(new Option<string?>(
                aliases: ["--token", "-t"],
                description: "The token of the Jenkins to connect to. Can also be provided via an environment variable: 'Jenkins__Token'"));
        }
    }

    internal abstract class BaseCommandHandler : ICommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public Uri? Uri { get; set; }

        public string? Username { get; set; }

        public string? Token { get; set; }

        public abstract int Invoke(InvocationContext context);

        public abstract Task<int> InvokeAsync(InvocationContext context);
    }
}