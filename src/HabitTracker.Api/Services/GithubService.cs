
using System.Net.Http;
using HabitTracker.Api.DTOs.Github;
using Newtonsoft.Json;

namespace HabitTracker.Api.Services;

public sealed class GithubService(IHttpClientFactory httpClientFactory, ILogger<GithubService> logger)
{
    public async Task<GithubUserProfileDto> GetUserProfileAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using HttpClient httpClient = CreateGithubClient(accessToken);

        HttpResponseMessage response = await httpClient.GetAsync("user", cancellationToken);

        if(!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch GitHub user profile. Status Code: {StatusCode}", response.StatusCode);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        GithubUserProfileDto userProfile = JsonConvert.DeserializeObject<GithubUserProfileDto>(content);
        return userProfile;
        
    }

    public async Task<IReadOnlyList<GithubEventDto>?> GetUserEventsAsync(string username, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        using HttpClient httpClient = CreateGithubClient(accessToken);
        HttpResponseMessage response = await httpClient.GetAsync($"users/{username}/events?per_page=100", cancellationToken);
        if(!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch GitHub user events. Status Code: {StatusCode}", response.StatusCode);
            return null;
        }
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        IReadOnlyList<GithubEventDto> events = JsonConvert.DeserializeObject<IReadOnlyList<GithubEventDto>>(content);
        return events;
    }

    private HttpClient CreateGithubClient(string accessToken)
    {
        HttpClient client = httpClientFactory.CreateClient("github");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }
}
