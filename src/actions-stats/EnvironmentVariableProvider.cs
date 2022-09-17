using System;

namespace ActionsStats;

public class EnvironmentVariableProvider
{
    private const string GH_PAT = "GH_PAT";
    private const string BBS_USERNAME = "BBS_USERNAME";
    private const string BBS_PASSWORD = "BBS_PASSWORD";
    private const string AZURE_STORAGE_CONNECTION_STRING = "AZURE_STORAGE_CONNECTION_STRING";

    private readonly OctoLogger _logger;

    private readonly Func<string, string> _getEnvironmentVariable;

    public EnvironmentVariableProvider(OctoLogger logger) : this(logger, v => Environment.GetEnvironmentVariable(v))
    {
    }

    internal EnvironmentVariableProvider(OctoLogger logger, Func<string, string> getEnvironmentVariable)
    {
        _logger = logger;
        _getEnvironmentVariable = getEnvironmentVariable;
    }

    public virtual string GithubPersonalAccessToken() =>
            GetSecret(GH_PAT)
            ?? throw new ActionsStatsException($"{GH_PAT} environment variable is not set.");

    public virtual string BbsUsername() =>
            GetSecret(BBS_USERNAME)
            ?? throw new ActionsStatsException($"{BBS_USERNAME} environment variable is not set.");

    public virtual string BbsPassword() =>
            GetSecret(BBS_PASSWORD)
            ?? throw new ActionsStatsException($"{BBS_PASSWORD} environment variable is not set.");

    public virtual string AzureStorageConnectionString() =>
            GetSecret(AZURE_STORAGE_CONNECTION_STRING)
            ?? throw new ActionsStatsException($"{AZURE_STORAGE_CONNECTION_STRING} environment variable is not set.");

    private string GetSecret(string secretName)
    {
        var secret = _getEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secret))
        {
            return null;
        }

        _logger?.RegisterSecret(secret);

        return secret;
    }
}
