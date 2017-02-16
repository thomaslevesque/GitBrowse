using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitBrowse
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var remoteName = args.Length > 0 ? args[0] : GetDefaultRemoteName();
            string url = string.IsNullOrEmpty(remoteName) ? null : GetRemoteUrl(remoteName);
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Remote not found");
            }
            OpenUrl(url);
        }

        private static string GetDefaultRemoteName()
        {
            // TODO: use current tracking branch if possible

            var remoteNames = GetRemoteNames();
            return remoteNames
                .OrderBy(n => n.Equals("origin", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();
        }

        private static string[] GetRemoteNames()
        {
            return GetGitCommandOutput("remote")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetGitCommandOutput(string command)
        {
            var psi = new ProcessStartInfo("git", command)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                    throw new Exception($"Failed to start 'git {command}'");

                process.WaitForExit();
                if (process.ExitCode == 0)
                    return process.StandardOutput.ReadToEnd();

                string error = process.StandardError.ReadToEnd();
                string message =
                    $"Command 'git {command}' exited with code {process.ExitCode}. Error output: {error}";
                throw new Exception(message);
            }
        }

        private static string GetRemoteUrl(string remoteName)
        {
            return GetGitCommandOutput($"config remote.{remoteName}.url");
        }

        private static void OpenUrl(string url)
        {
            var uri = RemoveDotGitSuffix(EnsureValidWebUri(url));

            var psi = new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            };
            Process.Start(psi)?.Dispose();
        }

        private static Uri EnsureValidWebUri(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                var builder = new UriBuilder(uri);
                if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    builder.Scheme = "https";
                }
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    builder.UserName = string.Empty;
                    builder.Password = string.Empty;
                }
                return builder.Uri;
            }

            if (TryGitUriToHttps(url, out uri))
                return uri;

            throw new Exception("Failed to obtain a valid web URI for the remote");
        }

        private static bool TryGitUriToHttps(string gitUri, out Uri uri)
        {
            var m = Regex.Match(gitUri, @"[a-zA-Z0-9-_]+\@(?<host>[a-zA-Z0-9-_\.]+):(?<path>.+)");
            if (m.Success)
            {
                var builder = new UriBuilder
                {
                    Scheme = "https",
                    Host = m.Groups["host"].Value,
                    Path = m.Groups["path"].Value
                };
                uri = builder.Uri;
                return true;
            }
            uri = null;
            return false;
        }

        private static Uri RemoveDotGitSuffix(Uri uri)
        {
            var builder = new UriBuilder(uri);
            if (builder.Path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                builder.Path = builder.Path.Substring(0, builder.Path.Length - 4);
                return builder.Uri;
            }
            return uri;
        }
    }
}