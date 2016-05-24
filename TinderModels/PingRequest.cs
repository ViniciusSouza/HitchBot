using System.Runtime.Serialization;
using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    public class PingRequest
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public async Task Ping()
        {
            await Client.Post("user/ping", this);
        }
    }
}