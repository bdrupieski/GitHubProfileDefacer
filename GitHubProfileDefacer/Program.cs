using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;

namespace GitHubProfileDefacer
{
    class Program
    {
        /// <summary>
        /// Sunday evening hack. Please don't judge the quality of this code. 
        /// I only care that it works.
        /// </summary>
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

                const string theFileToEdit = "something.txt";
                string fileInRepo = Path.Combine(repo.Info.WorkingDirectory, theFileToEdit);

                DateTime dateForTopLeftOfGithubProfileMatrix = FigureOutFirstSundayOfOneYearAgo();
                int howManyDaysWeveCommitted = 0;
                var r = new Random();

                const int repeatThePatternThisManyTimes = 6;
                const int commitsPerDay = 10;

                for (int p = 0; p < repeatThePatternThisManyTimes; p++)
                {
                    for (int i = 0; i < lines.First().Length; i++)
                    {
                        for (int j = 0; j < 7; j++)
                        {
                            var surveySays = lines[j][i];
                            if (surveySays == '.')
                            {
                                for (int k = 0; k < commitsPerDay; k++)
                                {
                                    File.WriteAllText(fileInRepo, r.Next().ToString(CultureInfo.InvariantCulture));
                                    repo.Index.Stage(theFileToEdit);

                                    DateTime commitDate = dateForTopLeftOfGithubProfileMatrix +
                                                          TimeSpan.FromDays(howManyDaysWeveCommitted);
                                    Signature author = new Signature(githubName, githubEmail, commitDate);
                                    Signature committer = author;

                                    // it would be cool to generate realistic-sounding commit messages here,
                                    // maybe using markov chains built from the text of other commit messages
                                    // scraped from github or something
                                    repo.Commit("hello", author, committer);
                                }
                            }

                            howManyDaysWeveCommitted++;
                        }
                    }
                }
            }
        }

        public static DateTime FigureOutFirstSundayOfOneYearAgo()
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