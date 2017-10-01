using System.IO;
using Newtonsoft.Json;

namespace GitHubProfileDefacer.Common
{
    public class GitHubCredentials
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }

        public static GitHubCredentials GetGitHubCredentials(string credentialsFilename = "credentials.json")
        {
            return JsonConvert.DeserializeObject<GitHubCredentials>(File.ReadAllText(credentialsFilename));
        }
    }
}