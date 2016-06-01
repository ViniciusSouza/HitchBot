using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    public class MessageRequest
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        public async Task<Msg> Send(string matchid)
        {
            Msg response = await Client.Post<Msg>($"user/matches/{matchid}", this);
            return response;
        }
    }
}