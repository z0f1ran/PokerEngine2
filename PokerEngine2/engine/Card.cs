using System;
using System.Collections.Generic;

public class Card
{
    public const int CLUB = 2;
    public const int DIAMOND = 4;
    public const int HEART = 8;
    public const int SPADE = 16;

    public static readonly Dictionary<int, char> SUIT_MAP = new Dictionary<int, char>
    {
        {2, 'C'},
        {4, 'D'},
        {8, 'H'},
        {16, 'S'}
    };

    public static readonly Dictionary<int, string> RANK_MAP = new Dictionary<int, string>
    {
        {2, "2"},
        {3, "3"},
        {4, "4"},
        {5, "5"},
        {6, "6"},
        {7, "7"},
        {8, "8"},
        {9, "9"},
        {10, "T"},
        {11, "J"},
        {12, "Q"},
        {13, "K"},
        {14, "A"}
    };

    public int suit;
    public int rank;

    public Card(int suit, int rank)
    {
        this.suit = suit;
        this.rank = (rank == 1) ? 14 : rank;
    }

    public bool Equals(Card other)
    {
        return this.suit == other.suit && this.rank == other.rank;
    }

    public override string ToString()
    {
        char suitChar = SUIT_MAP[suit];
        string rankStr = RANK_MAP[rank];
        return $"{suitChar}{rankStr}";
    }

    public int ToId()
    {
        int tmp = suit >> 1;
        int num = 0;
        while ((tmp & 1) != 1)
        {
            num++;
            tmp >>= 1;
        }

        int adjustedRank = (rank == 14) ? 1 : rank;
        return adjustedRank + 13 * num;
    }

    public static Card FromId(int cardId)
    {
        int suit = 2;
        int rank = cardId;
        while (rank > 13)
        {
            suit <<= 1;
            rank -= 13;
        }

        return new Card(suit, rank);
    }

    public static Card FromString(string strCard)
    {
        if (strCard.Length != 2)
        {
            throw new ArgumentException("Input string must have length 2.");
        }

        Dictionary<char, int> inverseSuitMap = new Dictionary<char, int>(SUIT_MAP.Count);
        foreach (var entry in SUIT_MAP)
        {
            inverseSuitMap[entry.Value] = entry.Key;
        }

        Dictionary<string, int> inverseRankMap = new Dictionary<string, int>(RANK_MAP.Count);
        foreach (var entry in RANK_MAP)
        {
            inverseRankMap[entry.Value] = entry.Key;
        }

        char suitChar = Char.ToUpper(strCard[0]);
        int suit = inverseSuitMap[suitChar];
        string rankStr = strCard[1].ToString();
        int rank = inverseRankMap[rankStr];

        return new Card(suit, rank);
    }
}
