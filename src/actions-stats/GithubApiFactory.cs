using System.Net.Http;

namespace ActionsStats;

public class GithubApiFactory
{
    private const string DEFAULT_API_URL = "https://api.github.com";

    private readonly OctoLogger _octoLogger;
    private readonly HttpClient _client;
    private readonly EnvironmentVariableProvider _environmentVariableProvider;
    private readonly RetryPolicy _retryPolicy;
    private readonly IVersionProvider _versionProvider;
    private readonly DateTimeProvider _dateTimeProvider;

    public GithubApiFactory(OctoLogger octoLogger, HttpClient client, EnvironmentVariableProvider environmentVariableProvider, RetryPolicy retryPolicy, IVersionProvider versionProvider, DateTimeProvider dateTimeProvider)
    {
        _octoLogger = octoLogger;
        _client = client;
        _environmentVariableProvider = environmentVariableProvider;
        _retryPolicy = retryPolicy;
        _versionProvider = versionProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public virtual GithubApi Create(string apiUrl = null, string targetPersonalAccessToken = null, bool proxima = false)
    {
        apiUrl ??= DEFAULT_API_URL;
        if (proxima)
        {
            apiUrl = "https://api.github.ghe.com";
        }
        targetPersonalAccessToken ??= _environmentVariableProvider.GithubPersonalAccessToken();
        var githubClient = new GithubClient(_octoLogger, _client, _retryPolicy, _versionProvider, _dateTimeProvider, targetPersonalAccessToken);
        return new GithubApi(githubClient, apiUrl, _retryPolicy);
    }
}
