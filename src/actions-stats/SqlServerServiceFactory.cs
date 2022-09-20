using ActionStats.Services;

namespace ActionsStats;

public class SqlServerServiceFactory
{
    private readonly OctoLogger _octoLogger;
    private readonly EnvironmentVariableProvider _environmentVariableProvider;

    public SqlServerServiceFactory(OctoLogger octoLogger, EnvironmentVariableProvider environmentVariableProvider)
    {
        _octoLogger = octoLogger;
        _environmentVariableProvider = environmentVariableProvider;
    }

    public virtual SqlServerService Create(string sqlConnectionString = null)
    {
        sqlConnectionString ??= _environmentVariableProvider.SqlConnectionString();
        return new SqlServerService(_octoLogger, sqlConnectionString);
    }
}
