using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GitHubProfileDefacer.Common;
using LibGit2Sharp;

namespace GitHubProfileDefacer
{
    class Program
    {
        static void Main(string[] args)
        {
            const string pathOfRepo = @"C:\wherever\yours\is";

            const string patternFile = "pattern.txt";

            var gitHubCredentials = GitHubCredentials.GetGitHubCredentials();

            string rootedPath = Repository.Init(pathOfRepo);
            Console.WriteLine("Using repo here: {0}", rootedPath);
            Console.WriteLine();

            using (var repo = new Repository(rootedPath))
            {
                if (!File.Exists(patternFile))
                    return;

                var lines = File.ReadAllLines(patternFile);

                if (lines.Length != 7 ||
                    lines.Any(line => line.Length != lines.First().Length) ||
                    lines.Any(line => !line.All(c => c == 'X' || c == '.')))
                {
                    Console.WriteLine("There should be 7 lines, each of equal length, and they should only have periods (.) or the letter X");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Going to make commits in this pattern:");
                Console.WriteLine(string.Join(Environment.NewLine, lines));

                const string theFileToEdit = "file.txt";
                string fileInRepo = Path.Combine(repo.Info.WorkingDirectory, theFileToEdit);

                DateTime dateForTopLeftOfGithubProfileMatrix = FirstSundayOfOneYearAgo();
                int weeksFromLeftToStart = 23;
                DateTime startingDate = dateForTopLeftOfGithubProfileMatrix + TimeSpan.FromDays(7 * weeksFromLeftToStart);
                int howManyDaysWeveCommitted = 0;
                var r = new Random();

                var commitMessages = CommitMessages.Get();

                const int commitsPerDay = 6;

                for (int i = 0; i < lines.First().Length; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        var patternCharacter = lines[j][i];
                        if (patternCharacter == '.')
                        {
                            DateTime commitDate = startingDate + TimeSpan.FromDays(howManyDaysWeveCommitted);
                            Signature author = new Signature(gitHubCredentials.Username, gitHubCredentials.Email, commitDate);
                            Signature committer = author;

                            for (int k = 0; k < commitsPerDay; k++)
                            {
                                File.WriteAllText(fileInRepo, r.Next().ToString(CultureInfo.InvariantCulture));

                                repo.Stage(theFileToEdit);
                                repo.Commit(commitMessages.NextCommitMessage(), author, committer);
                            }
                        }

                        howManyDaysWeveCommitted++;
                    }
                }
            }
        }

        public static DateTime FirstSundayOfOneYearAgo()
        {
            DateTime aYearAgoSunday = DateTime.Now - TimeSpan.FromDays(365);

            while (aYearAgoSunday.DayOfWeek != DayOfWeek.Sunday)
            {
                aYearAgoSunday += TimeSpan.FromDays(1);
            }

            return aYearAgoSunday;
        }

        class CommitMessages
        {
            private readonly List<string> _commitMessages;
            private readonly Random _r  = new Random();

            public CommitMessages(List<string> commitMessages)
            {
                _commitMessages = commitMessages;
            }

            public string NextCommitMessage()
            {
                var randomIndex = _r.Next(0, _commitMessages.Count);
                return _commitMessages[randomIndex];
            }

            public static CommitMessages Get()
            {
                List<string> commitMessages;
                var fakeCommitFilename = "sentences.txt";
                if (File.Exists(fakeCommitFilename))
                {
                    var lines = File.ReadAllLines(fakeCommitFilename);
                    commitMessages = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                }
                else
                {
                    commitMessages = new List<string> { "." };
                }

                return new CommitMessages(commitMessages);
            }
        }
    }
}