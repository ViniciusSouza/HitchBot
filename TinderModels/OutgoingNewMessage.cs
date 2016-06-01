using Newtonsoft.Json;

namespace TinderModels
{
    public class OutgoingNewMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}