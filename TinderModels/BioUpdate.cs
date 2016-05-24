using System.Threading.Tasks;
using TinderTinderApi;

namespace TinderModels
{
    /// <summary>
    /// Updates the profile biography text
    /// </summary>
    public class BioUpdate
    {
        public string Bio { get; set; }

        public async Task SaveProfile()
        {
            await Client.Post("profile", this);
        }
    }
}