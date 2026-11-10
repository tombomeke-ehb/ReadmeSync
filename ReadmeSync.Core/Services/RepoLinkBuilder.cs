namespace ReadmeSync.Services
{
    /// <summary>
    /// Builds browsable links to source files in a hosted repository.
    /// </summary>
    public static class RepoLinkBuilder
    {
        /// <summary>
        /// Returns the URL of a file, or null when no usable repository URL was given.
        /// GitHub, GitLab and Bitbucket repository URLs get the path segment those hosts
        /// need to show a file (for example /blob/HEAD/); any other URL is used as a plain prefix.
        /// </summary>
        public static string? BuildFileUrl(string? repoUrl, string relativePath, string? branch = null)
        {
            if (string.IsNullOrWhiteSpace(repoUrl))
                return null;

            string baseUrl = repoUrl.Trim().TrimEnd('/');
            if (baseUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                baseUrl = baseUrl[..^4];

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            string path = string.Join("/", relativePath
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

            string reference = string.IsNullOrWhiteSpace(branch) ? "HEAD" : branch.Trim();
            bool isRepoRoot = uri.AbsolutePath.Trim('/').Split('/').Length == 2;
            string host = uri.Host.ToLowerInvariant();

            if (isRepoRoot)
            {
                if (host is "github.com" or "www.github.com")
                    return $"{baseUrl}/blob/{reference}/{path}";
                if (host.Contains("gitlab"))
                    return $"{baseUrl}/-/blob/{reference}/{path}";
                if (host is "bitbucket.org" or "www.bitbucket.org")
                    return $"{baseUrl}/src/{reference}/{path}";
            }

            return $"{baseUrl}/{path}";
        }
    }
}
