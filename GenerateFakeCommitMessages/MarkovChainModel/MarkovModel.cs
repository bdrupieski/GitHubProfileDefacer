using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenerateFakeCommitMessages.MarkovChainModel
{
    public class MarkovModel
    {
        private readonly Random _r = new Random();

        private IDictionary<Digram, Dictionary<string, int>> _frequencies;

        private MarkovModel() { }

        public static MarkovModel Build(IList<string> passages)
        {
            var allTrigrams = GenerateTrigrams(passages);
            var m = new MarkovModel
            {
                _frequencies = GenerateFrequenciesFromTrigrams(allTrigrams)
            };

            return m;
        }

        private static IEnumerable<Trigram> GenerateTrigrams(IEnumerable<string> passages)
        {
            var space = new[] {' '};
            var startAndEndBuffer = new[] { "", "" };

            var allTrigrams = new ConcurrentBag<Trigram>();
            var partitions = Partitioner.Create(passages);
            Parallel.ForEach(partitions, passage =>
            {
                var words = passage.Split(space, StringSplitOptions.RemoveEmptyEntries);
                var wordsWithBuffers = startAndEndBuffer.Concat(words).Concat(startAndEndBuffer);
                var wordsInGroupsOf3 = wordsWithBuffers.Windowed(3);

                foreach (var group in wordsInGroupsOf3)
                {
                    var wordsInGroup = (IList<string>) group;
                    var trigram = new Trigram
                    {
                        First = wordsInGroup[0],
                        Second = wordsInGroup[1],
                        Third = wordsInGroup[2]
                    };
                    allTrigrams.Add(trigram);
                }
            });

            return allTrigrams;
        }

        private static IDictionary<Digram, Dictionary<string, int>> GenerateFrequenciesFromTrigrams(IEnumerable<Trigram> trigrams)
        {
            // cool but slow
            //return trigrams
            //    .AsParallel()
            //    .Select(x => new {Digram = new Digram(x.First, x.Second), third = x.Third})
            //    .GroupBy(x => x.Digram)
            //    .ToDictionary(x => x.Key, y => y.GroupBy(z => z.third).ToDictionary(a => a.Key, b => b.Count()));

            var thirdWordFrequencies = new Dictionary<Digram, Dictionary<string, int>>();

            foreach (var trigram in trigrams)
            {
                var digram = new Digram(trigram.First, trigram.Second);

                if (!thirdWordFrequencies.ContainsKey(digram))
                {
                    thirdWordFrequencies[digram] = new Dictionary<string, int>();
                }

                if (!thirdWordFrequencies[digram].ContainsKey(trigram.Third))
                {
                    thirdWordFrequencies[digram][trigram.Third] = 1;
                }
                else
                {
                    thirdWordFrequencies[digram][trigram.Third]++;
                }
            }

            return thirdWordFrequencies;
        }


        private string MostLikelyNextWord(IDictionary<Digram, Dictionary<string, int>> wordFreq, Digram digram)
        {
            return wordFreq[digram].OrderByDescending(x => x.Value).First().Key;
        }

        private string ProbableNextWord(IDictionary<Digram, Dictionary<string, int>> wordFreq, Digram digram)
        {
            var words = wordFreq[digram].OrderBy(x => x.Value);
            int runningSum = 0;

            var wordsWithCumulativeFreq = new List<Tuple<string, int>>();
            foreach (var keyValuePair in words)
            {
                runningSum += keyValuePair.Value;
                wordsWithCumulativeFreq.Add(Tuple.Create(keyValuePair.Key, runningSum));
            }

            var threshold = _r.NextDouble() * runningSum;
            var probableWord = wordsWithCumulativeFreq.First(x => x.Item2 > threshold);

            return probableWord.Item1;
        }

        private string RandomNextWord(IDictionary<Digram, Dictionary<string, int>> wordFreq, Digram digram)
        {
            var words = wordFreq[digram].ToArray();
            var randomWordIndex = _r.Next(0, words.Length);
            var randomWord = words[randomWordIndex];
            return randomWord.Key;
        }

        private string GenerateSentence(Func<IDictionary<Digram, Dictionary<string, int>>, Digram, string> nextWordFunc)
        {
            var sentenceWords = new List<string>();

            var digram = new Digram("", ""); // first word matches pattern ["", "", "some word"]
            var nextWord = nextWordFunc(_frequencies, digram);
            sentenceWords.Add(nextWord);

            while (nextWord != "")
            {
                digram = new Digram(digram.Second, nextWord);
                nextWord = nextWordFunc(_frequencies, digram);
                sentenceWords.Add(nextWord);
            }

            return string.Join(" ", sentenceWords);
        }

        public string GenerateMostLikelySentence()
        {
            return GenerateSentence(MostLikelyNextWord);
        }

        public string GenerateProbableSentence()
        {
            return GenerateSentence(ProbableNextWord);
        }

        public string GenerateRandomSentence()
        {
            return GenerateSentence(RandomNextWord);
        }
    }
}