using Newtonsoft.Json;
using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    public class ReccommendationsRequest
    {
        [JsonProperty("results")]
        public UserResult[] Results { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        public static async Task<ReccommendationsRequest> GetRecommendations()
        {
            return await Client.GetAsync<ReccommendationsRequest>("user/recs").ConfigureAwait(false);
        }
    }
}