using System;

namespace GitHubProfileDefacer.GenerateFakeCommitMessages.MarkovChainModel
{
    public struct Digram : IEquatable<Digram>
    {
        public Digram(string first, string second)
        {
            First = first;
            Second = second;
        }

        public string First { get; }
        public string Second { get; }

        public bool Equals(Digram other)
        {
            return string.Equals(First, other.First) && string.Equals(Second, other.Second);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Digram && Equals((Digram) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (First.GetHashCode()*397) ^ Second.GetHashCode();
            }
        }

        public static bool operator ==(Digram left, Digram right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Digram left, Digram right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"[{First}, {Second}]";
        }
    }
}