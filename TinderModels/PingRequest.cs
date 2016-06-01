using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    public class PingRequest
    {

        [JsonProperty(PropertyName = "lat")]
        public double Latitude { get; set; }

        [JsonProperty(PropertyName = "lon")]
        public double Longitude { get; set; }

        public async Task Ping()
        {
            await Client.Post("user/ping", this);
        }
    }
}