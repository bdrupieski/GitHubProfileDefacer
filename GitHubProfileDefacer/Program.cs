using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace GitHubProfileDefacer
{
    class Program
    {
        static void Main(string[] args)
        {
            const string githubName = "your_github_name_here";
            const string githubEmail = "your_github_email_here";
            const string pathOfRepo = @"C:\wherever\yours\is";

            const string patternFile = "pattern.txt";

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
                DateTime startingDate = dateForTopLeftOfGithubProfileMatrix + TimeSpan.FromDays(7 * 4);
                int howManyDaysWeveCommitted = 0;
                var r = new Random();

                const int commitsPerDay = 6;

                for (int i = 0; i < lines.First().Length; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        var patternCharacter = lines[j][i];
                        if (patternCharacter == '.')
                        {
                            DateTime commitDate = startingDate + TimeSpan.FromDays(howManyDaysWeveCommitted);
                            Signature author = new Signature(githubName, githubEmail, commitDate);
                            Signature committer = author;

                            for (int k = 0; k < commitsPerDay; k++)
                            {
                                File.WriteAllText(fileInRepo, r.Next().ToString(CultureInfo.InvariantCulture));

                                repo.Index.Stage(theFileToEdit);
                                repo.Commit(".", author, committer);
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
    }
}