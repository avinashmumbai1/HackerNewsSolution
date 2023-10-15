using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HackerNewsAPI.Domain;
using HackerNewsAPI.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;

namespace HackerNewsAPI.Services
{
    public class StoryService : IStoryService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public StoryService(IHttpClientFactory clientFactory, IMemoryCache cache, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _cache = cache;
            _configuration = configuration;

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable) // Handle 503 Service Unavailable
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public async Task<List<StoryDto>> GetNewestStoriesAsync(int pageNumber, int pageSize)
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["HackerNewsApi:BaseUrl"]);
            var newStoriesEndpoint = _configuration["HackerNewsApi:NewStoriesEndpoint"];
            var itemEndpoint = _configuration["HackerNewsApi:ItemEndpoint"];

            var storyIdsResponse = await _retryPolicy.ExecuteAsync(() => client.GetAsync(newStoriesEndpoint));
            storyIdsResponse.EnsureSuccessStatusCode();
            var storyIds = await storyIdsResponse.Content.ReadFromJsonAsync<List<int>>();

            var stories = new List<StoryDto>();

            foreach (var storyId in storyIds.Skip((pageNumber - 1) * pageSize).Take(pageSize))
            {
                var storyResponse = await _retryPolicy.ExecuteAsync(() => client.GetAsync($"{itemEndpoint}/{storyId}.json"));
                if (storyResponse.IsSuccessStatusCode)
                {
                    var content = await storyResponse.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(content);

                    string title = jObject["title"]?.ToString();
                    string url = jObject["url"]?.ToString();
                    string by = jObject["by"]?.ToString() ?? "Unknown";
                    int descendants = jObject["descendants"]?.Value<int>() ?? 0;
                    int score = jObject["score"]?.Value<int>() ?? 0;
                    long timeUnixEpoch = jObject["time"]?.Value<long>() ?? 0;
                    string type = jObject["type"]?.ToString() ?? "Unknown";

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(url))
                    {
                        var storyDto = new StoryDto
                        {
                            Id = storyId,
                            Title = title,
                            Url = url,
                            By = by,
                            Descendants = descendants,
                            Score = score,
                            Time = DateTimeOffset.FromUnixTimeSeconds(timeUnixEpoch).UtcDateTime,
                            Type = type,
                            TimeUnixEpoch = timeUnixEpoch
                        };

                        stories.Add(storyDto);
                    }
                }
            }

            return stories;
        }

        public async Task<int> GetTotalStoriesCountAsync()
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["HackerNewsApi:BaseUrl"]);
            var bestStoriesEndpoint = _configuration["HackerNewsApi:BestStoriesEndpoint"]; // Use BestStoriesEndpoint

            var storyIdsResponse = await _retryPolicy.ExecuteAsync(() => client.GetAsync(bestStoriesEndpoint)); // Fetch top 200 best stories
            storyIdsResponse.EnsureSuccessStatusCode();
            var storyIds = await storyIdsResponse.Content.ReadFromJsonAsync<List<int>>();

            return storyIds.Count;
        }


        public async Task<List<StoryDto>> SearchStoriesAsync(string query)
        {
            string cacheKey = $"SearchStories_{query}";
            if (_cache.TryGetValue(cacheKey, out List<StoryDto> cachedResults))
            {
                return cachedResults;
            }

            var newestStories = await GetNewestStoriesAsync(1, 200);
            var searchResults = newestStories
                .Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _cache.Set(cacheKey, searchResults);
            return searchResults;
        }

        public async Task<(List<StoryDto> Stories, int TotalPages)> SearchStoriesAsync(string query, int pageNumber, int pageSize)
        {
            // Fetch all newest stories
            var newestStories = await GetNewestStoriesAsync(1, 200);

            // Perform search and paginate the results
            var searchResults = newestStories
                .Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            int totalResults = searchResults.Count;
            int totalPages = (int)Math.Ceiling((double)totalResults / pageSize);
            var pagedResults = searchResults.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return (pagedResults, totalPages);
        }
    }
}
