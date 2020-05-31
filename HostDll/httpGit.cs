using Octokit;
using System.Net.Http;
using System.Net;

namespace kOFRRepo
{
    internal class httpGit
    {
        public static HttpClient httpClient = new HttpClient();
        public static GitHubClient githubClient = new GitHubClient(new ProductHeaderValue("kOFReadie-RepoApp"));
        public static WebClient webClient = new WebClient();

        public static void setupClients()
        {
            httpClient.DefaultRequestHeaders.Add("user-agent", "kOFReadie-RepoApp");
            webClient.Headers.Add("user-agent", "kOFReadie-RepoApp");
        }
    }
}
