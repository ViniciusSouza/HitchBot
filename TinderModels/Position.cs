using Newtonsoft.Json;

namespace TinderModels
{
    public class Position
    {
        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonProperty("lon")]
        public double Longitude { get; set; }
    }
}