using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ActionsStats.Extensions;
using ActionsStats.Models;

namespace ActionsStats.Commands;

public class GetRunsCommand : Command
{
    private readonly OctoLogger _log;
    private readonly IVersionProvider _versionProvider;
    private readonly FileSystemProvider _fileSystemProvider;
    private readonly EnvironmentVariableProvider _environmentVariableProvider;
    private readonly GithubApiFactory _apiFactory;

    public GetRunsCommand(
        OctoLogger log,
        IVersionProvider versionProvider,
        FileSystemProvider fileSystemProvider,
        EnvironmentVariableProvider environmentVariableProvider,
        GithubApiFactory apiFactory) : base("get-runs")
    {
        _log = log;
        _versionProvider = versionProvider;
        _fileSystemProvider = fileSystemProvider;
        _environmentVariableProvider = environmentVariableProvider;
        _apiFactory = apiFactory;

        Description = "Gets a list of all workflow runs and outputs it to a CSV file";

        var org = new Option<string>("--org") { IsRequired = true };
        var repo = new Option<string>("--repo") { IsRequired = true };
        var workflowId = new Option<string>("--workflow-id")
        {
            IsRequired = false,
            Description = "The ID of the workflow to get the list of runs for."
        };
        var workflowName = new Option<string>("--workflow-name")
        {
            IsRequired = false,
            Description = "Name of the workflow as displayed on the Actions tab."
        };
        var actor = new Option<string>("--actor")
        {
            IsRequired = false,
            Description = "Filter workflow runs by the actor associated with the runs."
        };
        var githubPat = new Option<string>("--github-pat")
        {
            IsRequired = false,
            Description = "Can also be provided using the GH_PAT environment variable."
        };
        var output = new Option<FileInfo>("--output", () => new FileInfo("./actions-runs.csv")) { IsRequired = false };
        var sqlConnectionString = new Option<string>("--sql-connection-string")
        {
            IsRequired = false,
            Description = "SQL Server connection string for where to write the data",
        };
        var verbose = new Option<bool>("--verbose") { IsRequired = false };

        AddOption(org);
        AddOption(repo);
        AddOption(workflowId);
        AddOption(workflowName);
        AddOption(actor);
        AddOption(githubPat);
        AddOption(output);
        AddOption(sqlConnectionString);
        AddOption(verbose);

        Handler = CommandHandler.Create<GetRunsCommandArgs>(Invoke);
    }

    public async Task Invoke(GetRunsCommandArgs args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        _log.Verbose = args.Verbose;

        _log.LogInformation("Getting Actions Runs...");

        LogOptions(args);
        ValidateOptions(args);

        args.GithubPat = args.GithubPat.HasValue() ? args.GithubPat : _environmentVariableProvider.GithubPersonalAccessToken();
        args.SqlConnectionString = args.SqlConnectionString.HasValue() ? args.SqlConnectionString : _environmentVariableProvider.SqlConnectionString();

        _log.RegisterSecret(args.GithubPat);
        _log.RegisterSecret(args.SqlConnectionString);

        var api = _apiFactory.Create(apiUrl: null, args.GithubPat);

        if (args.WorkflowName.HasValue())
        {
            args.WorkflowId = await api.GetWorkflowId(args.Org, args.Repo, args.WorkflowName);
        }

        var runs = await api.GetWorkflowRuns(args.Org, args.Repo, args.WorkflowId, args.Actor);

        var csv = GenerateCsv(runs);

        if (csv.HasValue() && args.Output.HasValue())
        {
            await _fileSystemProvider.WriteAllTextAsync(args.Output.FullName, csv);
        }
    }

    private string GenerateCsv(IEnumerable<WorkflowRun> runs)
    {
        var result = new StringBuilder();

        result.AppendLine("org,repo,workflow-id,workflow-name,actor,date,conclusion");

        foreach (var run in runs)
        {
            result.AppendLine($"\"{run.Org}\",\"{run.Repo}\",{run.WorkflowId},\"{run.WorkflowName}\",\"{run.Actor}\",\"{run.RunDate:dd-MMM-yyyy hh:mm tt}\",\"{run.Conclusion}\"");
        }

        return result.ToString();
    }

    private void ValidateOptions(GetRunsCommandArgs args)
    {
        if (!args.WorkflowId.HasValue() && !args.WorkflowName.HasValue())
        {
            throw new ActionsStatsException("Only provide one of workflow-id or workflow-name");
        }
    }

    private void LogOptions(GetRunsCommandArgs args)
    {
        _log.LogInformation($"ORG: {args.Org}");
        _log.LogInformation($"REPO: {args.Repo}");

        if (args.WorkflowId.HasValue())
        {
            _log.LogInformation($"WORKFLOW ID: {args.WorkflowId}");
        }

        if (args.WorkflowName.HasValue())
        {
            _log.LogInformation($"WORKFLOW NAME: {args.WorkflowName}");
        }

        if (args.Actor.HasValue())
        {
            _log.LogInformation($"ACTOR: {args.Actor}");
        }

        if (args.GithubPat.HasValue())
        {
            _log.LogInformation("GITHUB PAT: ***");
        }

        if (args.Output.HasValue())
        {
            _log.LogInformation($"OUTPUT: {args.Output}");
        }
    }
}

public class GetRunsCommandArgs
{
    public string Org { get; set; }
    public string Repo { get; set; }
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; }
    public string Actor { get; set; }
    public string GithubPat { get; set; }
    public FileInfo Output { get; set; }
    public string SqlConnectionString { get; set; }
    public bool Verbose { get; set; }
}
