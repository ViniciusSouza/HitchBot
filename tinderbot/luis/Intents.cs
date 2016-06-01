using Newtonsoft.Json;

namespace tinderbot.luis
{
    public class Intents
    {
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }
    }
}