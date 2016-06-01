using Newtonsoft.Json;

namespace TinderModels
{
    public class UserResponse
    {
        [JsonProperty("results")]
        public UserResult Results { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }
}