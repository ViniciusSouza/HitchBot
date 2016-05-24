using System.Runtime.Serialization;
using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    public class AuthRequest
    {
        public string FacebookID { get; set; }

        public string FacebookToken { get; set; }

        public async Task<AuthResponse> Send()
        {
            AuthResponse response = await Client.Post<AuthResponse>("auth", this);
            Client.AuthToken = response.AuthToken;
            return response;
        }
    }
}