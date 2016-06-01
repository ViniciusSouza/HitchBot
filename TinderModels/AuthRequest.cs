using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    public class AuthRequest
    {
        [JsonProperty(PropertyName = "facebook_id")]
        public string FacebookID { get; set; }

        [JsonProperty(PropertyName = "facebook_token")]
        public string FacebookToken { get; set; }

        public async Task<AuthResponse> Send()
        {
            AuthResponse response = await Client.Post<AuthResponse>("auth", this);
            Client.AuthToken = response.AuthToken;
            return response;
        }
    }
}