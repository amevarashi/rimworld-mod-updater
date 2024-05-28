namespace RimWorldModUpdater.Models;

public struct ModSourceInfo
{
    public string SourceType { get; set; }

    // HttpGet
    public string HttpGetDownloadLink { get; set; }
    public string HttpGetAboutLink { get; set; }
    public string HttpGetManifestLink { get; set; }

    // GitLab
    public string GitLabServer { get; set; }
    public string Repository { get; set; }
    public string Branch { get; set; }
}