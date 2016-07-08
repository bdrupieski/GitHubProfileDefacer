using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GenerateFakeCommitMessages.MarkovChainModel
{
    public class MarkovModel
    {
        private readonly Random _r = new Random();

        private Dictionary<Digram, Dictionary<string, int>> _frequencies;

        private MarkovModel() { }

        public static MarkovModel Build(IEnumerable<string> passages)
        {
            var allTrigrams = GenerateTrigrams(passages);
            var m = new MarkovModel
            {
                _frequencies = GenerateFrequenciesFromTrigrams(allTrigrams)
            };

            return m;
        }

        private static List<Trigram> GenerateTrigrams(IEnumerable<string> passages)
        {
            var allTrigrams = new List<Trigram>();
            Regex sentenceSplitter = new Regex(@"(?<=[.\!\?])\s+(?=[A-Z])");

            foreach (var passage in passages)
            {
                var sentences = sentenceSplitter.Split(passage);

                foreach (var sentence in sentences)
                {
                    var words = sentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var empty = new[] { "", "" };
                    var wordsWithBuffers = empty.Concat(words).Concat(empty);

                    var wordsInGroupsOf3 = wordsWithBuffers.Windowed(3);

                    var trigramsForSentence = new List<Trigram>();
                    foreach (var group in wordsInGroupsOf3)
                    {
                        var wordsInGroup = @group.ToArray();
                        if (wordsInGroup.Length >= 3)
                        {
                            var trigram = new Trigram
                            {
                                First = wordsInGroup[0],
                                Second = wordsInGroup[1],
                                Third = wordsInGroup[2]
                            };
                            trigramsForSentence.Add(trigram);
                        }
                    }

                    allTrigrams.AddRange(trigramsForSentence);
                }
            }

            return allTrigrams;
        }

        private static Dictionary<Digram, Dictionary<string, int>> GenerateFrequenciesFromTrigrams(IEnumerable<Trigram> trigrams)
        {
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


        private string MostLikelyNextWord(Dictionary<Digram, Dictionary<string, int>> wordFreq, Digram digram)
        {
            return wordFreq[digram].OrderByDescending(x => x.Value).First().Key;
        }

        private string ProbableNextWord(Dictionary<Digram, Dictionary<string, int>> wordFreq, Digram digram)
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

        private string RandomNextWord(Dictionary<Digram, Dictionary<string, int>> wordFreq, Digram digram)
        {
            var words = wordFreq[digram].ToArray();
            var randomWordIndex = _r.Next(0, words.Length);
            var randomWord = words[randomWordIndex];
            return randomWord.Key;
        }

        private string GenerateSentence(Func<Dictionary<Digram, Dictionary<string, int>>, Digram, string> nextWordFunc)
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