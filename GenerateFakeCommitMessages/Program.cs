using System;
using Octokit;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using GenerateFakeCommitMessages.MarkovChainModel;
using JsonNet.PrivateSettersContractResolvers;
using Newtonsoft.Json;

namespace GenerateFakeCommitMessages
{
    class Program
    {
        static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver(),
                Formatting = Formatting.Indented
            };

            var client = GetClient("credentials.json");

            var languages = new[]
            {
                Language.Python,
                Language.JavaScript,
                Language.Java,
                Language.CSharp,
                Language.Scala,
                Language.Ruby,
                Language.CPlusPlus,
                Language.ObjectiveC
            };

            foreach (var language in languages)
            {
                var langFolder = CreateOrGetLanguageFolder(language);
                var repositories = LoadRepositoriesFromDiskOrSearchForThem(client, langFolder, language);

                var allCommitMessages = new List<string>();
                foreach (var repository in repositories)
                {
                    var commitMessages = LoadCommitMessagesFromDiskOrSearchForThem(client, langFolder, repository);
                    Console.WriteLine($"retrieved {commitMessages.Count} commit messages from ${repository.Name}");
                    var splitMessages = commitMessages.SelectMany(x => x.Split(new[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries));
                    allCommitMessages.AddRange(splitMessages);
                }

                var markovModel = MarkovModel.Build(allCommitMessages);
                var sb = new StringBuilder();
                sb.AppendLine(language.ToString());
                sb.AppendLine($"Most probable sentence: {markovModel.GenerateMostLikelySentence()}");
                sb.AppendLine();
                sb.AppendLine("Likely sentences:");
                for (int i = 0; i < 5000; i++)
                {
                    sb.AppendLine(markovModel.GenerateProbableSentence());
                }
                var sentenceReportFilename = Path.Combine(langFolder.FullName, "sentences.txt");
                File.WriteAllText(sentenceReportFilename, sb.ToString());
            }
        }

        static GitHubClient GetClient(string credentialsFilename)
        {
            var credentials = JsonConvert.DeserializeObject<MyGitHubCredentials>(File.ReadAllText(credentialsFilename));
            var tokenAuth = new Credentials(credentials.Token);
            return new GitHubClient(new ProductHeaderValue(credentials.Username)) { Credentials = tokenAuth };
        }

        static DirectoryInfo CreateOrGetLanguageFolder(Language language)
        {
            var langName = language.ToString();
            return Directory.Exists(langName) 
                ? new DirectoryInfo(langName) 
                : Directory.CreateDirectory(langName);
        }

        static List<string> LoadCommitMessagesFromDiskOrSearchForThem(GitHubClient client, DirectoryInfo langFolder, Repository repository)
        {
            var filename = Path.Combine(langFolder.FullName, $"{repository.Owner.Login},{repository.Name}.json");
            List<string> commitMessages;

            if (File.Exists(filename))
            {
                commitMessages = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filename));
            }
            else
            {
                List<GitHubCommit> commits = null;
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
                        Console.WriteLine($"Hit rate limit. Waiting until {exception.Reset.LocalDateTime}");
                        while (DateTimeOffset.UtcNow < exception.Reset)
                        {
                            Thread.Sleep(30000);
                        }
                    }
                    catch (AggregateException e) when (e.InnerException is ApiException &&
                                                       ((ApiException)e.InnerException).StatusCode == HttpStatusCode.InternalServerError)
                    {
                        break;
                    }
                }

                commitMessages = commits?.Select(x => x.Commit.Message).ToList() ?? new List<string>();
                var json = JsonConvert.SerializeObject(commitMessages);

                File.WriteAllText(filename, json);
            }

            return commitMessages;
        }

        static List<Repository> LoadRepositoriesFromDiskOrSearchForThem(GitHubClient client, DirectoryInfo langFolder, Language language)
        {
            var filename = Path.Combine(langFolder.FullName, "repos.json");
            List<Repository> repos;

            if (File.Exists(filename))
            {
                repos = JsonConvert.DeserializeObject<List<Repository>>(File.ReadAllText(filename));
            }
            else
            {
                repos = SearchForAllRepositories(client, language);
                var json = JsonConvert.SerializeObject(repos);
                File.WriteAllText(filename, json);
            }

            return repos;
        }

        static List<Repository> SearchForAllRepositories(GitHubClient client, Language language)
        {
            var searchRepoRequest = new SearchRepositoriesRequest
            {
                Language = language,
                Stars = Range.GreaterThan(30),
            };

            var results = new List<Repository>();

            var repos = client.Search.SearchRepo(searchRepoRequest).Result;
            results.AddRange(repos.Items.ToList());

            int totalItems = repos.TotalCount;

            // search restricted to first 1000 results, so stop after ten 100 result pages
            while (results.Count < totalItems && repos.Items.Any() && searchRepoRequest.Page < 10)
            {
                searchRepoRequest.Page++;

                repos = client.Search.SearchRepo(searchRepoRequest).Result;
                results.AddRange(repos.Items.ToList());
            }

            return results;
        }
    }
}
