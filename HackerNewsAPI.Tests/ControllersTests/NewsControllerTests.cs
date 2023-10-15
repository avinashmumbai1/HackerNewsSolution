using Xunit;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YourNamespace.Controllers;
using HackerNewsAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;

namespace HackerNewsAPI.Tests
{
    public class NewsControllerTests
    {
        [Fact]
        public async Task GetNewStories_ReturnsOkObjectResult()
        {
            // Arrange
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClientMock = new Mock<HttpClient>();
            var memoryCacheMock = new Mock<IMemoryCache>();
            var configurationMock = new Mock<IConfiguration>();
            var storyServiceMock = new Mock<IStoryService>();

            httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("[1, 2, 3]") // Sample response from the API
                });

            httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(httpClientMock.Object);

            var controller = new NewsController(httpClientFactoryMock.Object, memoryCacheMock.Object, configurationMock.Object, storyServiceMock.Object);

            // Act
            var result = await controller.GetNewStories();

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        //[Fact]
        //public async Task GetStoryById_ExistingStory_ReturnsOkObjectResult()
        //{
        //    // Arrange
        //    var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        //    var httpClientMock = new Mock<HttpClient>();
        //    var memoryCacheMock = new Mock<IMemoryCache>();
        //    var configurationMock = new Mock<IConfiguration>();
        //    var storyServiceMock = new Mock<IStoryService>();
        //    var loggerMock = new Mock<ILogger<NewsController>>();

        //    var storyId = 1;
        //    var storyDto = new StoryDTO { Id = storyId, Title = "Sample Story", Url = "https://example.com" };

        //    memoryCacheMock.Setup(cache => cache.TryGetValue(storyId, out It.IsAny<StoryDTO>()))
        //        .Returns(true);

        //    memoryCacheMock.Setup(cache => cache.TryGetValue(storyId, out storyDto))
        //        .Returns(true);

        //    var controller = new NewsController(httpClientFactoryMock.Object, memoryCacheMock.Object, configurationMock.Object, storyServiceMock.Object)
        //    {
        //        Logger = loggerMock.Object
        //    };

        //    // Act
        //    var result = await controller.GetStoryById(storyId);

        //    // Assert
        //    Assert.IsType<OkObjectResult>(result.Result);
        //}
    }
}
