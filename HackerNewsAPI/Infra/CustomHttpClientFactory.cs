namespace HackerNewsAPI.Infra
{
    public class CustomHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public CustomHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }
}
