using Newtonsoft.Json;

namespace TinderModels.Facebook
{
    public class PhotosResponse
    {
        [JsonProperty("data")]
        public PhotoResponse[] Data { get; set; }
    }
}