using System.Collections.Generic;

namespace EmbyKinopoiskTrailers.ScheduledTasks.Model
{
    internal sealed class GitHubLatestReleaseResponse
    {
        public string tag_name { get; set; }
        public string body { get; set; }
        public string html_url { get; set; }
        public List<GitHubLatestReleaseAsset> assets { get; set; } = new List<GitHubLatestReleaseAsset>();
    }
}
