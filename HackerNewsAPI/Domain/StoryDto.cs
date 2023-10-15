using System.Text.Json.Serialization;

namespace HackerNewsAPI.Domain
{
    public class StoryDto
    {


        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string By { get; set; }
        public int Descendants { get; set; }
        public int Score { get; set; }

        [JsonIgnore]
        public DateTimeOffset Time { get; set; }  // Use long data type for Unix timestamps
        public string? Type { get; set; }
        [JsonPropertyName("time")]
        public long TimeUnixEpoch { get; set; }
    }
}
