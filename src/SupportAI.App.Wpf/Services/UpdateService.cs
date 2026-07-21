using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace SupportAI.App.Wpf.Services;

public static class UpdateService
{
    public static async Task<(bool IsUpdateAvailable, string? LatestVersion, string? ReleaseUrl)> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "SupportAI-USB-App");
            
            var url = "https://api.github.com/repos/moisesvalero/SupportAI-USB/releases/latest";
            var release = await client.GetFromJsonAsync<GitHubRelease>(url);
            
            if (!string.IsNullOrWhiteSpace(release?.tag_name))
            {
                var latestVersionStr = release.tag_name.TrimStart('v', 'V');
                if (Version.TryParse(latestVersionStr, out var latestVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (currentVersion != null && latestVersion > currentVersion)
                    {
                        return (true, release.tag_name, release.html_url);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[UpdateService] Error al buscar actualizaciones: {ex.Message}");
        }
        return (false, null, null);
    }

    private class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? html_url { get; set; }
    }
}
