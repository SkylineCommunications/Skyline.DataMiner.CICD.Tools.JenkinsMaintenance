namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.SystemCommandLine
{
    using System.CommandLine.Parsing;

    using Skyline.DataMiner.CICD.FileSystem.FileSystemInfoWrapper;

    using DirectoryInfo = Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper.DirectoryInfo;
    using FileInfo = Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper.FileInfo;

    /// <summary>
    /// Helper methods so that System.CommandLine can deal with CICD.FileSystem classes.
    /// </summary>
    internal static class OptionHelper
    {
        public static IFileSystemInfoIO? ParseFileSystemInfo(ArgumentResult result)
        {
            if (result.Tokens.Count != 1)
            {
                result.ErrorMessage = $"--{result.Argument.Name} requires exactly one argument.";
                return null;
            }

            string tokenValue = result.Tokens[0].Value;
            if (FileSystem.FileSystem.Instance.File.GetAttributes(tokenValue).HasFlag(System.IO.FileAttributes.Directory))
            {
                return new DirectoryInfo(tokenValue);
            }

            return new FileInfo(tokenValue);
        }

        public static DirectoryInfo? ParseDirectoryInfo(ArgumentResult result)
        {
            if (result.Tokens.Count != 1)
            {
                result.ErrorMessage = $"--{result.Argument.Name} requires exactly one argument.";
                return null;
            }

            return new DirectoryInfo(result.Tokens[0].Value);
        }

        public static FileInfo? ParseFileInfo(ArgumentResult result)
        {
            if (result.Tokens.Count != 1)
            {
                result.ErrorMessage = $"--{result.Argument.Name} requires exactly one argument.";
                return null;
            }

            return new FileInfo(result.Tokens[0].Value);
        }
    }
}