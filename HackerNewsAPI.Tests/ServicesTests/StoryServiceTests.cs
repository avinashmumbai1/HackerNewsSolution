//using Xunit;
//using Moq;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Http;
//using System.Threading.Tasks;
//using HackerNewsAPI.Domain;
//using HackerNewsAPI.Interfaces;
//using HackerNewsAPI.Services;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;

//namespace HackerNewsAPI.Tests.ServicesTests
//{
//    public class StoryServiceTests
//    {
//        [Fact]
//        public async Task GetNewestStoriesAsync_ReturnsListOfStories()
//        {
//            // Arrange
//            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
//            var configurationMock = new Mock<IConfiguration>();
//            var memoryCacheMock = new Mock<IMemoryCache>();

//            var httpClientMock = new Mock<HttpClient>();
//            httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
//                .Returns(httpClientMock.Object);

//            var storyService = new StoryService(httpClientFactoryMock.Object, memoryCacheMock.Object, configurationMock.Object);

//            // Mock the HttpClient behavior
//            var storyIds = new List<int> { 1, 2, 3 }; // Sample story IDs
//            var json = "[{\"title\":\"Title 1\",\"url\":\"http://example.com/1\"}," +
//                       "{\"title\":\"Title 2\",\"url\":\"http://example.com/2\"}," +
//                       "{\"title\":\"Title 3\",\"url\":\"http://example.com/3\"}]";

//            httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>()))
//                .ReturnsAsync(new HttpResponseMessage
//                {
//                    StatusCode = HttpStatusCode.OK,
//                    Content = new StringContent(json)
//                });

//            // Act
//            var result = await storyService.GetNewestStoriesAsync(1, 3);

//            // Assert
//            Assert.NotNull(result);
//            Assert.IsType<List<StoryDto>>(result);
//            Assert.Equal(3, result.Count);
//        }

//        [Fact]
//        public async Task SearchStoriesAsync_ReturnsFilteredStories()
//        {
//            // Arrange
//            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
//            var configurationMock = new Mock<IConfiguration>();
//            var memoryCacheMock = new Mock<IMemoryCache>();

//            var httpClientMock = new Mock<HttpClient>();
//            httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
//                .Returns(httpClientMock.Object);

//            var storyService = new StoryService(httpClientFactoryMock.Object, memoryCacheMock.Object, configurationMock.Object);

//            // Mock the HttpClient behavior
//            var json = "[{\"title\":\"Test 1\",\"url\":\"http://example.com/1\"}," +
//                       "{\"title\":\"Test 2\",\"url\":\"http://example.com/2\"}," +
//                       "{\"title\":\"Test 3\",\"url\":\"http://example.com/3\"}]";

//            httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>()))
//                .ReturnsAsync(new HttpResponseMessage
//                {
//                    StatusCode = HttpStatusCode.OK,
//                    Content = new StringContent(json)
//                });

//            // Act
//            var result = await storyService.SearchStoriesAsync("Test");

//            // Assert
//            Assert.NotNull(result);
//            Assert.IsType<List<StoryDto>>(result);
//            Assert.Equal(3, result.Count);
//        }

//        [Fact]
//        public async Task GetTotalStoriesCountAsync_ReturnsTotalCount()
//        {
//            // Arrange
//            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
//            var configurationMock = new Mock<IConfiguration>();
//            var memoryCacheMock = new Mock<IMemoryCache>();

//            var httpClientMock = new Mock<HttpClient>();
//            httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
//                .Returns(httpClientMock.Object);

//            var storyService = new StoryService(httpClientFactoryMock.Object, memoryCacheMock.Object, configurationMock.Object);

//            // Mock the HttpClient behavior
//            var json = "[1, 2, 3, 4, 5]"; // Sample story IDs
//            httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>()))
//                .ReturnsAsync(new HttpResponseMessage
//                {
//                    StatusCode = HttpStatusCode.OK,
//                    Content = new StringContent(json)
//                });

//            // Act
//            var result = await storyService.GetTotalStoriesCountAsync();

//            // Assert
//            Assert.Equal(5, result);
//        }


//    }
//}

using Xunit;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HackerNewsAPI.Domain;
using HackerNewsAPI.Interfaces;
using HackerNewsAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace HackerNewsAPI.Tests.ServicesTests
{
    public class StoryServiceTests
    {
        [Fact]
        public async Task GetNewestStoriesAsync_ReturnsListOfStories()
        {
            // Arrange
            var httpClientWrapperMock = new Mock<IHttpClientWrapper>();
            var configurationMock = new Mock<IConfiguration>();
            var memoryCacheMock = new Mock<IMemoryCache>();

            // Mock the behavior of GetAsync method in IHttpClientWrapper
            var json = "[{\"title\":\"Test 1\",\"url\":\"http://example.com/1\"}," +
                       "{\"title\":\"Test 2\",\"url\":\"http://example.com/2\"}," +
                       "{\"title\":\"Test 3\",\"url\":\"http://example.com/3\"}]";

            httpClientWrapperMock.Setup(wrapper => wrapper.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            var storyService = new StoryService(memoryCacheMock.Object, configurationMock.Object);

            // Act
            var result = await storyService.GetNewestStoriesAsync(1, 3);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<StoryDto>>(result);
            Assert.Equal(3, result.Count);
        }
    }

    public class MockHttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;

        public MockHttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return _httpClient.GetAsync(requestUri);
        }
    }
}

