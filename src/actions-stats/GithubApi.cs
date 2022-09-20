using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActionsStats.Models;
using Newtonsoft.Json.Linq;

namespace ActionsStats;

public class GithubApi
{
    private readonly GithubClient _client;
    private readonly string _apiUrl;
    private readonly RetryPolicy _retryPolicy;

    public GithubApi(GithubClient client, string apiUrl, RetryPolicy retryPolicy)
    {
        _client = client;
        _apiUrl = apiUrl;
        _retryPolicy = retryPolicy;
    }

    public virtual async Task<int> GetWorkflowId(string org, string repo, string workflowName)
    {
        var url = $"{_apiUrl}/repos/{org}/{repo}/actions/workflows";

        var data = await _client.GetAllAsync(url, x => (JArray)x["workflows"], _ => true, x => (Name: (string)x["name"], Id: (int)x["id"]));

        return data.First(x => x.Name.Equals(workflowName, StringComparison.OrdinalIgnoreCase)).Id;
    }

    public virtual async Task<IEnumerable<WorkflowRun>> GetWorkflowRuns(string org, string repo, long workflowId, string actor, string branch)
    {
        var url = $"{_apiUrl}/repos/{org}/{repo}/actions/runs";

        var data = await _client.GetAllAsync(
            url,
            x => (JArray)x["workflow_runs"],
            x => (int)x["workflow_id"] == workflowId && (actor is null || (string)x["actor"]["login"] == actor) && (branch is null || (string)x["head_branch"] == branch),
            x => new WorkflowRun
            {
                Org = org,
                Repo = repo,
                WorkflowId = workflowId,
                WorkflowName = (string)x["name"],
                Actor = (string)x["actor"]["login"],
                Branch = (string)x["head_branch"],
                Event = (string)x["event"],
                RunDate = (DateTime)x["created_at"],
                Conclusion = (string)x["conclusion"],
                Url = (string)x["html_url"],
                RunNumber = (long)x["run_number"],
                Id = (long)x["id"]
            });

        return data;
    }
}
