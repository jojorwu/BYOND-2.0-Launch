using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Launcher.Services
{
    public class UpdateService
    {
        private const string GhUser = "jojorwu";
        private const string GhRepo = "BYOND-2.0";

        public Version CurrentVersion { get; }
        public Release? LatestRelease { get; private set; }

        public UpdateService()
        {
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0");
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("BYOND2-Launcher"));
                var releases = await github.Repository.Release.GetAll(GhUser, GhRepo);
                LatestRelease = releases.FirstOrDefault(r => !r.Prerelease);

                if (LatestRelease == null)
                {
                    return false;
                }

                var latestVersion = new Version(LatestRelease.TagName.TrimStart('v'));
                return latestVersion > CurrentVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update check failed: {ex.Message}");
                return false;
            }
        }
    }
}
