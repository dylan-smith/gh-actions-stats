using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OctoshiftCLI.Extensions;

namespace OctoshiftCLI
{
    public class GithubApi
    {
        private readonly GithubClient _client;
        private readonly string _apiUrl;
        private readonly RetryPolicy _retryPolicy;

        private readonly Dictionary<string, string> _internalSchemaHeader =
            new() { { "GraphQL-schema", "internal" } };

        public GithubApi(GithubClient client, string apiUrl, RetryPolicy retryPolicy)
        {
            _client = client;
            _apiUrl = apiUrl;
            _retryPolicy = retryPolicy;
        }

        public virtual async Task AddAutoLink(string org, string repo, string keyPrefix, string urlTemplate)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException($"Invalid value for {nameof(keyPrefix)}");
            }
            if (string.IsNullOrWhiteSpace(urlTemplate))
            {
                throw new ArgumentException($"Invalid value for {nameof(urlTemplate)}");
            }

            var url = $"{_apiUrl}/repos/{org}/{repo}/autolinks";

            var payload = new
            {
                key_prefix = keyPrefix,
                url_template = urlTemplate.Replace(" ", "%20")
            };

            await _client.PostAsync(url, payload);
        }

        public virtual async Task<List<(int Id, string KeyPrefix, string UrlTemplate)>> GetAutoLinks(string org, string repo)
        {
            var url = $"{_apiUrl}/repos/{org}/{repo}/autolinks";

            return await _client.GetAllAsync(url)
                                .Select(al => ((int)al["id"], (string)al["key_prefix"], (string)al["url_template"]))
                                .ToListAsync();
        }

        private void EnsureSuccessGraphQLResponse(JObject response)
        {
            if (response.TryGetValue("errors", out var jErrors) && jErrors is JArray { Count: > 0 } errors)
            {
                var error = (JObject)errors[0];
                var errorMessage = error.TryGetValue("message", out var jMessage) ? (string)jMessage : null;
                throw new OctoshiftCliException($"{errorMessage ?? "UNKNOWN"}");
            }
        }
    }
}
