using System;

namespace GenerateFakeCommitMessages.MarkovChainModel
{
    public class Digram : IEquatable<Digram>
    {
        public Digram(string first, string second)
        {
            First = first;
            Second = second;
        }

        public static bool operator ==(Digram left, Digram right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Digram left, Digram right)
        {
            return !Equals(left, right);
        }

        public string First { get; }
        public string Second { get; }

        public bool Equals(Digram other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(First, other.First) && string.Equals(Second, other.Second);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Digram)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((First?.GetHashCode() ?? 0) * 397) ^ (Second?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return $"[ {First}, {Second}]";
        }
    }
}