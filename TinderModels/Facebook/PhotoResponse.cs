using Newtonsoft.Json;

namespace TinderModels.Facebook
{
    public class PhotoResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}