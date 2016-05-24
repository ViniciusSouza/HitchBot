using System.Runtime.Serialization;

namespace TinderModels
{
    public class AuthResponse
    {
        public string AuthToken { get; set; }

        public Globals GlobalVariables { get; set; }

        public User UserProfile { get; set; }

        public Versions VersionInfo { get; set; }
    }
}