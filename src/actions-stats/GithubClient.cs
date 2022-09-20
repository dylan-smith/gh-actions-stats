﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ActionsStats.Extensions;
using Newtonsoft.Json.Linq;

namespace ActionsStats;

public class GithubClient
{
    private readonly HttpClient _httpClient;
    private readonly OctoLogger _log;
    private readonly RetryPolicy _retryPolicy;

    public GithubClient(OctoLogger log, HttpClient httpClient, IVersionProvider versionProvider, RetryPolicy retryPolicy, string personalAccessToken)
    {
        _log = log;
        _httpClient = httpClient;
        _retryPolicy = retryPolicy;

        if (_httpClient != null)
        {
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            if (versionProvider?.GetVersionComments() is { } comments)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(comments));
            }
        }
    }

    public virtual async Task<string> GetNonSuccessAsync(string url, HttpStatusCode status) => (await SendAsync(HttpMethod.Get, url, status: status)).Content;

    public virtual async Task<string> GetAsync(string url, Dictionary<string, string> customHeaders = null)
    {
        var (content, _) = await _retryPolicy.HttpRetry(
            async () => await SendAsync(HttpMethod.Get, url, customHeaders: customHeaders),
            _ => true
        );

        return content;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync<T>(string url, Func<JToken, JArray> data, Func<JToken, bool> predicate, Func<JToken, T> selector)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var page = 1;

        var (content, _) = await SendAsync(HttpMethod.Get, $"{url}?page={page}&per_page=100");

        var results = data(JObject.Parse(content))
            .Where(predicate)
            .Select(selector).ToList();
        var totalCount = (int)JObject.Parse(content)["total_count"];

        while ((page * 100) < totalCount)
        {
            page++;

            _log.LogInformation($"Retrieving Page {page} / {Math.Ceiling(totalCount / 100.0)}...");

            (content, _) = await SendAsync(HttpMethod.Get, $"{url}?page={page}&per_page=100");
            var newResults = data(JObject.Parse(content))
                .Where(predicate)
                .Select(selector).ToList();
            results = results.Union(newResults).ToList();
        }

        return results;
    }

    public virtual async Task<string> PostAsync(string url, object body, Dictionary<string, string> customHeaders = null) =>
        (await SendAsync(HttpMethod.Post, url, body, customHeaders: customHeaders)).Content;

    public virtual async IAsyncEnumerable<JToken> PostGraphQLWithPaginationAsync(
        string url,
        object body,
        Func<JObject, JArray> resultCollectionSelector,
        Func<JObject, JObject> pageInfoSelector,
        int first = 100,
        string after = null,
        Dictionary<string, string> customHeaders = null)
    {
        if (resultCollectionSelector is null)
        {
            throw new ArgumentNullException(nameof(resultCollectionSelector));
        }

        if (pageInfoSelector is null)
        {
            throw new ArgumentNullException(nameof(pageInfoSelector));
        }

        var jBody = JObject.FromObject(body);
        jBody["variables"] ??= new JObject();
        jBody["variables"]["first"] = first;

        var hasNextPage = true;
        while (hasNextPage)
        {
            jBody["variables"]["after"] = after;

            var (content, _) = await SendAsync(HttpMethod.Post, url, jBody, customHeaders: customHeaders);
            var jContent = JObject.Parse(content);
            foreach (var jResult in resultCollectionSelector(jContent))
            {
                yield return jResult;
            }

            var pageInfo = pageInfoSelector(jContent);
            if (pageInfo is null)
            {
                yield break;
            }

            hasNextPage = pageInfo["hasNextPage"]?.ToObject<bool>() ?? false;
            after = pageInfo["endCursor"]?.ToObject<string>();
        }
    }

    public virtual async Task<string> PutAsync(string url, object body, Dictionary<string, string> customHeaders = null) =>
        (await SendAsync(HttpMethod.Put, url, body, customHeaders: customHeaders)).Content;

    public virtual async Task<string> PatchAsync(string url, object body, Dictionary<string, string> customHeaders = null) =>
        (await SendAsync(HttpMethod.Patch, url, body, customHeaders: customHeaders)).Content;

    public virtual async Task<string> DeleteAsync(string url, Dictionary<string, string> customHeaders = null) => (await SendAsync(HttpMethod.Delete, url, customHeaders: customHeaders)).Content;

    private async Task<(string Content, KeyValuePair<string, IEnumerable<string>>[] ResponseHeaders)> SendAsync(
        HttpMethod httpMethod,
        string url,
        object body = null,
        HttpStatusCode status = HttpStatusCode.OK,
        Dictionary<string, string> customHeaders = null)
    {
        url = url?.Replace(" ", "%20");

        _log.LogVerbose($"HTTP {httpMethod}: {url}");

        using var request = new HttpRequestMessage(httpMethod, url).AddHeaders(customHeaders);

        if (body != null)
        {
            _log.LogVerbose($"HTTP BODY: {body.ToJson()}");

            request.Content = body.ToJson().ToStringContent();
        }

        using var response = await _httpClient.SendAsync(request);

        _log.LogVerbose($"GITHUB REQUEST ID: {ExtractHeaderValue("X-GitHub-Request-Id", response.Headers)}");
        var content = await response.Content.ReadAsStringAsync();
        _log.LogVerbose($"RESPONSE ({response.StatusCode}): ...");

        foreach (var header in response.Headers)
        {
            _log.LogDebug($"RESPONSE HEADER: {header.Key} = {string.Join(",", header.Value)}");
        }

        if (status == HttpStatusCode.OK)
        {
            response.EnsureSuccessStatusCode();
        }
        else if (response.StatusCode != status)
        {
            throw new HttpRequestException($"Expected status code {status} but got {response.StatusCode}", null, response.StatusCode);
        }

        return (content, response.Headers.ToArray());
    }

    private string ExtractHeaderValue(string key, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers) =>
        headers.SingleOrDefault(kvp => kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

}
