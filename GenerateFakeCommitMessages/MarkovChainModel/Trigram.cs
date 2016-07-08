namespace GenerateFakeCommitMessages.MarkovChainModel
{
    public class Trigram
    {
        public string First { get; set; }
        public string Second { get; set; }
        public string Third { get; set; }

        public override string ToString()
        {
            return $"[{First}, {Second}, {Third}]";
        }
    }
}