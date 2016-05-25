using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace TinderModels
{
    public class AuthResponse
    {
        [JsonProperty(PropertyName = "token")]
        public string AuthToken { get; set; }

        [JsonProperty(PropertyName = "globals")]
        public Globals GlobalVariables { get; set; }
        [JsonProperty(PropertyName = "user")]
        public User UserProfile { get; set; }

        [JsonProperty(PropertyName = "versions")]
        public Versions VersionInfo { get; set; }
    }
}