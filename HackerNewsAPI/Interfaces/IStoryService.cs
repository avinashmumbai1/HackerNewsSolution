using HackerNewsAPI.Domain;

namespace HackerNewsAPI.Interfaces
{
    public interface IStoryService
    {
        Task<List<StoryDto>> GetNewestStoriesAsync(int pageNumber, int pageSize);
        Task<List<StoryDto>> SearchStoriesAsync(string query);
        Task<int> GetTotalStoriesCountAsync();
        Task<(List<StoryDto> Stories, int TotalPages)> SearchStoriesAsync(string query, int pageNumber, int pageSize);
    }
}
