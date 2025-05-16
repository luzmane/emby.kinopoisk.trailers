namespace EmbyKinopoiskTrailers.ScheduledTasks.Model
{
    internal sealed class GitHubLatestReleaseAsset
    {
        internal const string CorrectContentType = "application/x-msdos-program";
        public string name { get; set; }
        public string browser_download_url { get; set; }
        public string content_type { get; set; }
    }
}
