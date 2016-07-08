using System;
using Octokit;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using JsonNet.PrivateSettersContractResolvers;
using Newtonsoft.Json;

namespace GenerateFakeCommitMessages
{
    class Program
    {
        static Language language = Language.CSharp;

        static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver(),
                Formatting = Formatting.Indented
            };

            var tokenAuth = new Credentials("token");
            var client = new GitHubClient(new ProductHeaderValue("nope")) { Credentials = tokenAuth };
            var repositories = LoadRepositoriesFromDiskOrSearchForThem(client);

            foreach (var repository in repositories)
            {
                var commits = LoadCommitMessagesFromDiskOrSearchForThem(client, repository);
                Console.WriteLine($"retrieved {commits.Count} commit messages from ${repository.Name}");
            }
        }

        static List<string> LoadCommitMessagesFromDiskOrSearchForThem(GitHubClient client, Repository repository)
        {
            var filename = $"{repository.Owner.Login},{repository.Name}.json";
            List<string> commitMessages;

            if (File.Exists(filename))
            {
                commitMessages = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filename));
            }
            else
            {
                List<GitHubCommit> commits;
                while (true)
                {
                    try
                    {
                        commits = client.Repository.Commit.GetAll(repository.Owner.Login, repository.Name).Result.ToList();
                        break;
                    }
                    catch (AggregateException e) when (e.InnerException is RateLimitExceededException)
                    {
                        var exception = (RateLimitExceededException)e.InnerException;
                        while (DateTimeOffset.UtcNow < exception.Reset)
                        {
                            Thread.Sleep(30000);
                        }
                    }
                }
                
                commitMessages = commits.Select(x => x.Commit.Message).ToList();
                var json = JsonConvert.SerializeObject(commitMessages);

                File.WriteAllText(filename, json);
            }

            return commitMessages;
        }

        static List<Repository> LoadRepositoriesFromDiskOrSearchForThem(GitHubClient client)
        {
            var filename = "repos.json";
            List<Repository> repos;

            if (File.Exists(filename))
            {
                repos = JsonConvert.DeserializeObject<List<Repository>>(File.ReadAllText(filename));
            }
            else
            {
                repos = SearchForAllRepositories(client);
                var json = JsonConvert.SerializeObject(repos);
                File.WriteAllText(filename, json);
            }

            return repos;
        }

        static List<Repository> SearchForAllRepositories(GitHubClient client)
        {
            var searchRepoRequest = new SearchRepositoriesRequest
            {
                Language = language,
                Stars = Range.GreaterThan(500),
            };

            var results = new List<Repository>();

            var repos = client.Search.SearchRepo(searchRepoRequest).Result;
            results.AddRange(repos.Items.ToList());

            int totalItems = repos.TotalCount;

            while (results.Count < totalItems && repos.Items.Any())
            {
                searchRepoRequest.Page++;

                repos = client.Search.SearchRepo(searchRepoRequest).Result;
                results.AddRange(repos.Items.ToList());
            }

            return results;
        }
    }
}
