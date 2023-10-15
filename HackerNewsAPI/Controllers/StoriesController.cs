using HackerNewsAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IStoryService _storyService;

        public StoriesController(IStoryService storyService)
        {
            _storyService = storyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNewestStories(int pageNumber = 1, int pageSize = 10)
        {
            var stories = await _storyService.GetNewestStoriesAsync(pageNumber, pageSize);
            return Ok(stories);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchStories(string query)
        {
            var stories = await _storyService.SearchStoriesAsync(query);
            return Ok(stories);
        }
    }

}
