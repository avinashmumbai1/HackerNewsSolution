using HackerNewsAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly List<StoryDTO> _stories;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        private readonly IStoryService _storyService;
        public NewsController(IHttpClientFactory httpClientFactory, 
                              IMemoryCache memoryCache, 
                              IConfiguration configuration,
                              IStoryService storyService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _cache = memoryCache;
            _configuration = configuration;
            _storyService   = storyService;
       ;
        }

        [HttpGet("newest")]
        public async Task<ActionResult<IEnumerable<StoryDTO>>> GetNewStories()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/beststories.json");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var storyIds = JsonSerializer.Deserialize<List<int>>(json);

                    List<StoryDTO> stories = new List<StoryDTO>();

                    foreach (var id in storyIds)
                    {
                        response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json");

                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                            var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true // Handle case-insensitive properties
                            });

                            // Check if essential properties are null, skip if they are
                            if (!string.IsNullOrEmpty(storyDto.Title) && !string.IsNullOrEmpty(storyDto.Url))
                            {
                                stories.Add(storyDto);
                            }
                        }
                    }

                    return Ok(stories);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        //[HttpGet("search")]
        //public async Task<ActionResult<IEnumerable<StoryDTO>>> SearchStories([FromQuery] string query)
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/newstories.json");

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var json = await response.Content.ReadAsStringAsync();
        //            var storyIds = JsonSerializer.Deserialize<List<int>>(json);

        //            // Fetch individual stories and filter based on the search query
        //            var filteredStories = new List<StoryDTO>();
        //            foreach (var id in storyIds)
        //            {
        //                response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json");

        //                if (response.IsSuccessStatusCode)
        //                {
        //                    json = await response.Content.ReadAsStringAsync();
        //                    var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
        //                    {
        //                        PropertyNameCaseInsensitive = true
        //                    });

        //                    // Check if essential properties are null or empty and match the search query
        //                    if (!string.IsNullOrEmpty(storyDto.Title) &&
        //                        !string.IsNullOrEmpty(storyDto.Url) &&
        //                        storyDto.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        filteredStories.Add(storyDto);
        //                    }
        //                }
        //            }

        //            return Ok(filteredStories);
        //        }
        //        else
        //        {
        //            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal Server Error: {ex.Message}");
        //    }
        //}

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<StoryDTO>>> SearchStories([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var (searchResults, totalPages) = await _storyService.SearchStoriesAsync(query, page, pageSize);
                return Ok(new { Stories = searchResults, TotalPages = totalPages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<StoryDTO>>> GetPagedStories([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/newstories.json");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var storyIds = JsonSerializer.Deserialize<List<int>>(json);

                    // Implement paging logic here
                    var startIndex = (page - 1) * pageSize;
                    var endIndex = Math.Min(startIndex + pageSize, storyIds.Count);

                    var pagedStories = new List<StoryDTO>();
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{storyIds[i]}.json");

                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                            var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            // Check if essential properties are null or empty
                            if (!string.IsNullOrEmpty(storyDto.Title) && !string.IsNullOrEmpty(storyDto.Url))
                            {
                                pagedStories.Add(storyDto);
                            }
                        }
                    }

                    return Ok(pagedStories);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        private async Task<List<StoryDTO>> FetchAndCacheStories(string endpoint)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var storyIds = JsonSerializer.Deserialize<List<int>>(json);

                var stories = new List<StoryDTO>();

                foreach (var id in storyIds)
                {
                    // Fetch individual story by ID from the Hacker News API
                    var itemEndpoint = _configuration.GetValue<string>("HackerNewsApi:ItemEndpoint").Replace("{id}", id.ToString());
                    response = await _httpClient.GetAsync(itemEndpoint);

                    if (response.IsSuccessStatusCode)
                    {
                        json = await response.Content.ReadAsStringAsync();
                        var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (!string.IsNullOrEmpty(storyDto?.Title) && !string.IsNullOrEmpty(storyDto?.Url))
                        {
                            stories.Add(storyDto);
                        }
                    }
                }

                // Cache the stories for 5 minutes (adjust the duration based on your requirements)
                _cache.Set(endpoint, stories, TimeSpan.FromMinutes(5));
                return stories;
            }
            else
            {
                throw new Exception($"API request failed with status code: {response.StatusCode}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StoryDTO>> GetStoryById(int id)
        {
            try
            {
                // Check if the story is already cached
                if (_cache.TryGetValue(id, out StoryDTO story))
                {
                    return Ok(story);
                }
                else
                {
                    // Fetch all new stories from the NewStoriesEndpoint
                    var newStoryEndpoint = _configuration.GetValue<string>("HackerNewsApi:TopStoriesEndpoint");
                    var stories = await FetchAndCacheStories(newStoryEndpoint);

                    // Find the story by ID from the fetched stories
                    var foundStory = stories.FirstOrDefault(s => s.Id == id);
                    if (foundStory != null)
                    {
                        // Cache the individual story for 5 minutes (adjust the duration based on your requirements)
                        _cache.Set(id, foundStory, TimeSpan.FromMinutes(5));
                        return Ok(foundStory);
                    }

                    // If the story is not found, return a custom 404 response
                    return NotFound($"Story with ID {id} was not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error", ex);
               // ILogger.LogError(ex, $"An error occurred while processing the request for story ID: {id}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("totalcount")]
        public async Task<ActionResult<int>> GetTotalStoriesCount()
        {
            try
            {
                var totalStoriesCount = await _storyService.GetTotalStoriesCountAsync();
                return Ok(totalStoriesCount);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 Internal Server Error response
                // logger.LogError(ex, "An error occurred while fetching total stories count.");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        // ... (other action methods and class properties)

    }

    public class StoryDTO
    {
        public string By { get; set; }
        public int Descendants { get; set; }
        public int Id { get; set; }
        public List<int> Kids { get; set; }
        public int Score { get; set; }
        public long Time { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        // Add other properties as needed from the API response
    }
}

//using HackerNewsAPI.Domain;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Polly;

//namespace YourNamespace.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class NewsController : ControllerBase
//    {
//        private readonly HttpClient _httpClient = new();
//        private readonly IMemoryCache _cache;
//        private readonly IConfiguration _configuration;


//        public NewsController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IConfiguration configuration)
//        {
//            _httpClient = httpClientFactory.CreateClient();
//            _cache = memoryCache;
//            _configuration = configuration;
//        }

//        [HttpGet("newest")]
//        public async Task<ActionResult<IEnumerable<StoryDTO>>> GetNewStories()
//        {
//            try
//            {
//                var cacheKey = "newest_stories";
//                if (!_cache.TryGetValue(cacheKey, out List<StoryDTO> stories))
//                {
//                    // Fetch data from the API and cache it
//                    stories = await FetchAndCacheStories(_configuration.GetValue<string>("HackerNewsApi:BaseUrl"));
//                }

//                return Ok(stories);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal Server Error: {ex.Message}");
//            }
//        }

//        [HttpGet("search")]
//        public async Task<ActionResult<IEnumerable<StoryDTO>>> SearchStories([FromQuery] string query)
//        {
//            try
//            {
//                var cacheKey = $"search_stories_{query}";
//                if (!_cache.TryGetValue(cacheKey, out List<StoryDTO> stories))
//                {
//                    // Fetch data from the API and cache it
//                    var searchEndpoint = _configuration.GetValue<string>("HackerNewsApi:SearchEndpoint").Replace("{query}", query);
//                    stories = await FetchAndCacheStories(searchEndpoint);
//                }

//                return Ok(stories);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal Server Error: {ex.Message}");
//            }
//        }

//        [HttpGet("paged")]
//        public async Task<ActionResult<IEnumerable<StoryDTO>>> GetPagedStories([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//        {
//            try
//            {
//                var cacheKey = $"paged_stories_{page}_{pageSize}";
//                if (!_cache.TryGetValue(cacheKey, out List<StoryDTO> stories))
//                {
//                    // Fetch data from the API and cache it
//                    var pagedEndpoint = _configuration.GetValue<string>("HackerNewsApi:PagedEndpoint")
//                        .Replace("{page}", page.ToString())
//                        .Replace("{pageSize}", pageSize.ToString());

//                    stories = await FetchAndCacheStories(pagedEndpoint);
//                }

//                return Ok(stories);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal Server Error: {ex.Message}");
//            }
//        }

//        //    private async Task<List<StoryDTO>> FetchAndCacheStories(string endpoint)
//        //    {
//        //        HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

//        //        if (response.IsSuccessStatusCode)
//        //        {
//        //            var json = await response.Content.ReadAsStringAsync();
//        //            var storyIds = JsonSerializer.Deserialize<List<int>>(json);

//        //            var stories = new List<StoryDTO>();

//        //            foreach (var id in storyIds)
//        //            {
//        //                response = await _httpClient.GetAsync(_configuration.GetValue<string>("HackerNewsApi:ItemEndpoint").Replace("{id}", id.ToString()));

//        //                if (response.IsSuccessStatusCode)
//        //                {
//        //                    json = await response.Content.ReadAsStringAsync();
//        //                    var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
//        //                    {
//        //                        PropertyNameCaseInsensitive = true
//        //                    });

//        //                    if (!string.IsNullOrEmpty(storyDto?.Title) && !string.IsNullOrEmpty(storyDto?.Url))
//        //                    {
//        //                        stories.Add(storyDto);
//        //                    }
//        //                }
//        //            }

//        //            // Cache the stories for 5 minutes (adjust the duration based on your requirements)
//        //            _cache.Set(endpoint, stories, TimeSpan.FromMinutes(5));
//        //            return stories;
//        //        }
//        //        else
//        //        {
//        //            throw new Exception($"API request failed with status code: {response.StatusCode}");
//        //        }
//        //    }
//        //}
//        //private async Task<List<StoryDTO>> FetchAndCacheStories(string endpoint)
//        //{
//        //    if (_cache.TryGetValue(endpoint, out List<StoryDTO> cachedStories))
//        //    {
//        //        return cachedStories;
//        //    }

//        //    HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

//        //    if (response.IsSuccessStatusCode)
//        //    {
//        //        var json = await response.Content.ReadAsStringAsync();
//        //        var storyIds = JsonSerializer.Deserialize<List<int>>(json);

//        //        var stories = new List<StoryDTO>();

//        //        foreach (var id in storyIds)
//        //        {
//        //            response = await _httpClient.GetAsync(_configuration.GetValue<string>("HackerNewsApi:ItemEndpoint").Replace("{id}", id.ToString()));

//        //            if (response.IsSuccessStatusCode)
//        //            {
//        //                json = await response.Content.ReadAsStringAsync();
//        //                var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
//        //                {
//        //                    PropertyNameCaseInsensitive = true
//        //                });

//        //                if (!string.IsNullOrEmpty(storyDto?.Title) && !string.IsNullOrEmpty(storyDto?.Url))
//        //                {
//        //                    stories.Add(storyDto);
//        //                }
//        //            }
//        //        }

//        //        // Cache the stories for 5 minutes (adjust the duration based on your requirements)
//        //        _cache.Set(endpoint, stories, TimeSpan.FromMinutes(5));
//        //        return stories;
//        //    }
//        //    else
//        //    {
//        //        throw new Exception($"API request failed with status code: {response.StatusCode}");
//        //    }
//        //}

//        //private async Task<List<StoryDTO>> FetchAndCacheStories(string endpoint)
//        //{
//        //    if (_cache.TryGetValue(endpoint, out List<StoryDTO> cachedStories))
//        //    {
//        //        return cachedStories;
//        //    }

//        //    HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

//        //    if (response.IsSuccessStatusCode)
//        //    {
//        //        var json = await response.Content.ReadAsStringAsync();
//        //        var storyIds = JsonSerializer.Deserialize<List<int>>(json);

//        //        var stories = new List<StoryDTO>();

//        //        foreach (var id in storyIds)
//        //        {
//        //            response = await _httpClient.GetAsync($"http://hacker-news.firebaseio.com/v0/item/{id}.json");

//        //            if (response.IsSuccessStatusCode)
//        //            {
//        //                json = await response.Content.ReadAsStringAsync();
//        //                var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
//        //                {
//        //                    PropertyNameCaseInsensitive = true
//        //                });

//        //                if (!string.IsNullOrEmpty(storyDto?.Title) && !string.IsNullOrEmpty(storyDto?.Url))
//        //                {
//        //                    stories.Add(storyDto);
//        //                }
//        //            }
//        //        }

//        //        // Cache the stories for 5 minutes (adjust the duration based on your requirements)
//        //        _cache.Set(endpoint, stories, TimeSpan.FromMinutes(5));
//        //        return stories;
//        //    }
//        //    else
//        //    {
//        //        throw new Exception($"API request failed with status code: {response.StatusCode}");
//        //    }
//        //}
//        //private async Task<List<StoryDTO>> FetchAndCacheStories(string endpoint)
//        //{
//        //    HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

//        //    if (response.IsSuccessStatusCode)
//        //    {
//        //        string json = await response.Content.ReadAsStringAsync();
//        //        var storyIds = JsonSerializer.Deserialize<List<int>>(json);

//        //        var stories = new List<StoryDTO>();

//        //        foreach (var id in storyIds)
//        //        {
//        //            //response = await _httpClient.GetAsync(_configuration.GetValue<string>("HackerNewsApi:ItemEndpoint").Replace("{id}", id.ToString()));

//        //            string itemEndpoint = $"http://hacker-news.firebaseio.com/v0/item/{id}.json";
//        //            response = await _httpClient.GetAsync(itemEndpoint);
//        //            if (response.IsSuccessStatusCode)
//        //            {
//        //                json = await response.Content.ReadAsStringAsync();
//        //                var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
//        //                {
//        //                    PropertyNameCaseInsensitive = true
//        //                });

//        //                if (IsValidStory(storyDto))
//        //                {
//        //                    stories.Add(storyDto);
//        //                }
//        //            }
//        //        }

//        //        // Cache the stories for 5 minutes (adjust the duration based on your requirements)
//        //        _cache.Set(endpoint, stories, TimeSpan.FromMinutes(5));
//        //        return stories;
//        //    }
//        //    else
//        //    {
//        //        throw new Exception($"API request failed with status code: {response.StatusCode}");
//        //    }
//        //}
//        private async Task<List<StoryDTO>> FetchAndCacheStories(string endpoint)
//        {
//            var retryPolicy = Policy
//                .Handle<HttpRequestException>()
//                .Or<IOException>() // Handle IOException separately
//                .RetryAsync(50, onRetry: (exception, retryCount, context) =>
//                {
//                    // Log or handle retries if needed
//                    Console.WriteLine($"Retry attempt {retryCount} due to exception: {exception.Message}");
//                });

//            return await retryPolicy.ExecuteAsync(async () =>
//            {
//                HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

//                if (response.IsSuccessStatusCode)
//                {
//                    string json = await response.Content.ReadAsStringAsync();
//                    var storyIds = JsonSerializer.Deserialize<List<int>>(json);

//                    var stories = new List<StoryDTO>();

//                    foreach (var id in storyIds)
//                    {
//                        string itemEndpoint = $"http://hacker-news.firebaseio.com/v0/item/{id}.json";
//                        var itemResponse = await _httpClient.GetAsync(itemEndpoint);

//                        if (itemResponse.IsSuccessStatusCode)
//                        {
//                            json = await itemResponse.Content.ReadAsStringAsync();
//                            if (json != "null")
//                            {
//                                var storyDto = JsonSerializer.Deserialize<StoryDTO>(json, new JsonSerializerOptions
//                                {
//                                    PropertyNameCaseInsensitive = true
//                                });

//                                if (IsValidStory(storyDto))
//                                {
//                                    stories.Add(storyDto);
//                                }
//                            }
//                        }
//                        else
//                        {
//                            // Handle non-successful response if necessary
//                            Console.WriteLine($"Non-successful response: {itemResponse.StatusCode}");
//                            // You can throw an exception here or handle the response based on your requirements.
//                        }
//                    }

//                    // Cache the stories for 5 minutes (adjust the duration based on your requirements)
//                    _cache.Set(endpoint, stories, TimeSpan.FromMinutes(5));
//                    return stories;
//                }
//                else
//                {
//                    throw new Exception($"API request failed with status code: {response.StatusCode}");
//                }
//            });
//        }

//        private bool IsValidStory(StoryDTO story)
//        {
//            return !string.IsNullOrEmpty(story?.Title) && !string.IsNullOrEmpty(story?.Url);
//        }

//        public class StoryDTO
//        {
//            public string By { get; set; }
//            public int Descendants { get; set; }
//            public int Id { get; set; }
//            public List<int> Kids { get; set; }
//            public int Score { get; set; }
//            public long Time { get; set; }
//            public string Title { get; set; }
//            public string Type { get; set; }
//            public string Url { get; set; }
//            // Add other properties as needed from the API response
//        }
//    }
//}
