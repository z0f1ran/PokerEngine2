using System;
using System.Collections.Generic;
using System.Linq;

public class HandEvaluator
{
    public const int HIGHCARD = 0;
    public const int ONEPAIR = 1 << 8;
    public const int TWOPAIR = 1 << 9;
    public const int THREECARD = 1 << 10;
    public const int STRAIGHT = 1 << 11;
    public const int FLASH = 1 << 12;
    public const int FULLHOUSE = 1 << 13;
    public const int FOURCARD = 1 << 14;
    public const int STRAIGHTFLASH = 1 << 15;

    public static Dictionary<int, string> HAND_STRENGTH_MAP = new Dictionary<int, string>
    {
        {HIGHCARD, "HIGHCARD"},
        {ONEPAIR, "ONEPAIR"},
        {TWOPAIR, "TWOPAIR"},
        {THREECARD, "THREECARD"},
        {STRAIGHT, "STRAIGHT"},
        {FLASH, "FLASH"},
        {FULLHOUSE, "FULLHOUSE"},
        {FOURCARD, "FOURCARD"},
        {STRAIGHTFLASH, "STRAIGHTFLASH"}
    };

    public static Dictionary<string, object> GenHandRankInfo(List<Card> hole, List<Card> community)
    {
        int hand = EvalHand(hole, community);
        int rowStrength = MaskHandStrength(hand);
        string strength = HAND_STRENGTH_MAP[rowStrength];
        int handHigh = MaskHandHighRank(hand);
        int handLow = MaskHandLowRank(hand);
        int holeHigh = MaskHoleHighRank(hand);
        int holeLow = MaskHoleLowRank(hand);

        return new Dictionary<string, object>
        {
            {
                "hand", new Dictionary<string, object>
                {
                    {"strength", strength},
                    {"high", handHigh},
                    {"low", handLow}
                }
            },
            {
                "hole", new Dictionary<string, object>
                {
                    {"high", holeHigh},
                    {"low", holeLow}
                }
            }
        };
    }

    public static int EvalHand(List<Card> hole, List<Card> community)
    {
        var ranks = hole.Select(card => card.rank).OrderBy(rank => rank).ToList();
        int holeFlg = ranks[1] << 4 | ranks[0];
        int handFlg = CalcHandInfoFlg(hole, community) << 8;
        return handFlg | holeFlg;
    }

    public static int CalcHandInfoFlg(List<Card> hole, List<Card> community)
    {
        var cards = hole.Concat(community).ToList();
        if (IsStraightFlash(cards)) return STRAIGHTFLASH | EvalStraightFlash(cards);
        if (IsFourCard(cards)) return FOURCARD | EvalFourCard(cards);
        if (IsFullHouse(cards)) return FULLHOUSE | EvalFullHouse(cards);
        if (IsFlash(cards)) return FLASH | EvalFlash(cards);
        if (IsStraight(cards)) return STRAIGHT | EvalStraight(cards);
        if (IsThreeCard(cards)) return THREECARD | EvalThreeCard(cards);
        if (IsTwoPair(cards)) return TWOPAIR | EvalTwoPair(cards);
        if (IsOnePair(cards)) return ONEPAIR | EvalOnePair(cards);
        return EvalHoleCard(hole);
    }

    public static int EvalHoleCard(List<Card> hole)
    {
        var ranks = hole.Select(card => card.rank).OrderBy(rank => rank).ToList();
        return ranks[1] << 4 | ranks[0];
    }

    public static bool IsOnePair(List<Card> cards)
    {
        return EvalOnePair(cards) != 0;
    }

    public static int EvalOnePair(List<Card> cards)
    {
        int rank = 0;
        int memo = 0; // bit memo
        foreach (var card in cards)
        {
            int mask = 1 << card.rank;
            if ((memo & mask) != 0) rank = Math.Max(rank, card.rank);
            memo |= mask;
        }
        return rank << 4;
    }

    public static bool IsTwoPair(List<Card> cards)
    {
        return SearchTwoPair(cards).Count == 2;
    }

    public static int EvalTwoPair(List<Card> cards)
    {
        var ranks = SearchTwoPair(cards);
        return ranks[0] << 4 | ranks[1];
    }

    public static List<int> SearchTwoPair(List<Card> cards)
    {
        var ranks = new List<int>();
        int memo = 0;
        foreach (var card in cards)
        {
            int mask = 1 << card.rank;
            if ((memo & mask) != 0) ranks.Add(card.rank);
            memo |= mask;
        }
        return ranks.OrderByDescending(rank => rank).Take(2).ToList();
    }

    public static bool IsThreeCard(List<Card> cards)
    {
        return SearchThreeCard(cards) != -1;
    }

    public static int EvalThreeCard(List<Card> cards)
    {
        return SearchThreeCard(cards) << 4;
    }

    public static int SearchThreeCard(List<Card> cards)
    {
        int rank = -1;
        int bitMemo = cards.Aggregate(0, (memo, card) => memo + (1 << (card.rank - 1) * 3));
        for (int r = 2; r <= 14; r++)
        {
            bitMemo >>= 3;
            int count = bitMemo & 7;
            if (count >= 3) rank = r;
        }
        return rank;
    }

    public static bool IsStraight(List<Card> cards)
    {
        return SearchStraight(cards) != -1;
    }

    public static int EvalStraight(List<Card> cards)
    {
        return SearchStraight(cards) << 4;
    }

    public static int SearchStraight(List<Card> cards)
    {
        int bitMemo = cards.Aggregate(0, (memo, card) => memo | 1 << card.rank);
        int rank = -1;
        Func<bool, int, bool> straightCheck = (acc, i) => acc && (bitMemo >> (rank + i) & 1) == 1;
        for (int r = 2; r <= 14; r++)
        {
            if (Enumerable.Range(0, 5).Aggregate(true, straightCheck)) rank = r;
        }
        return rank;
    }

    public static bool IsFlash(List<Card> cards)
    {
        return SearchFlash(cards) != -1;
    }

    public static int EvalFlash(List<Card> cards)
    {
        return SearchFlash(cards) << 4;
    }

    public static int SearchFlash(List<Card> cards)
    {
        int bestSuitRank = -1;
        Func<Card, int> fetchSuit = card => card.suit;
        Func<Card, int> fetchRank = card => card.rank;
        foreach (var group in cards.OrderBy(fetchSuit).GroupBy(fetchSuit))
        {
            var g = group.ToList();
            if (g.Count >= 5)
            {
                var maxRankCard = g.OrderByDescending(fetchRank).First();
                bestSuitRank = Math.Max(bestSuitRank, maxRankCard.rank);
            }
        }
        return bestSuitRank;
    }

    public static bool IsFullHouse(List<Card> cards)
    {
        var (r1, r2) = SearchFullHouse(cards);
        return r1 != 0 && r2 != 0;
    }

    public static int EvalFullHouse(List<Card> cards)
    {
        var (r1, r2) = SearchFullHouse(cards);
        return r1 << 4 | r2;
    }

    public static (int, int) SearchFullHouse(List<Card> cards)
    {
        Func<Card, int> fetchRank = card => card.rank;
        var threeCardRanks = new List<int>();
        var twoPairRanks = new List<int>();
        foreach (var group in cards.OrderBy(fetchRank).GroupBy(fetchRank))
        {
            var g = group.ToList();
            if (g.Count >= 3) threeCardRanks.Add(group.Key);
            if (g.Count >= 2) twoPairRanks.Add(group.Key);
        }
        twoPairRanks = twoPairRanks.Where(rank => !threeCardRanks.Contains(rank)).ToList();
        if (threeCardRanks.Count == 2) twoPairRanks.Add(threeCardRanks.Min());
        int Max(List<int> l) => l.Count == 0 ? 0 : l.Max();
        return (Max(threeCardRanks), Max(twoPairRanks));
    }

    public static bool IsFourCard(List<Card> cards)
    {
        return EvalFourCard(cards) != 0;
    }

    public static int EvalFourCard(List<Card> cards)
    {
        int rank = SearchFourCard(cards);
        return rank << 4;
    }

    public static int SearchFourCard(List<Card> cards)
    {
        Func<Card, int> fetchRank = card => card.rank;
        foreach (var group in cards.OrderBy(fetchRank).GroupBy(fetchRank))
        {
            var g = group.ToList();
            if (g.Count >= 4) return group.Key;
        }
        return 0;
    }

    public static bool IsStraightFlash(List<Card> cards)
    {
        return SearchStraightFlash(cards) != -1;
    }

    public static int EvalStraightFlash(List<Card> cards)
    {
        return SearchStraightFlash(cards) << 4;
    }

    public static int SearchStraightFlash(List<Card> cards)
    {
        var flashCards = new List<Card>();
        Func<Card, int> fetchSuit = card => card.suit;
        foreach (var group in cards.OrderBy(fetchSuit).GroupBy(fetchSuit))
        {
            var g = group.ToList();
            if (g.Count >= 5) flashCards = g;
        }
        return SearchStraight(flashCards);
    }

    public static int MaskHandStrength(int bit)
    {
        int mask = 511 << 16;
        return (bit & mask) >> 8; // 511 = (1 << 9) -1
    }

    public static int MaskHandHighRank(int bit)
    {
        int mask = 15 << 12;
        return (bit & mask) >> 12;
    }

    public static int MaskHandLowRank(int bit)
    {
        int mask = 15 << 8;
        return (bit & mask) >> 8;
    }

    public static int MaskHoleHighRank(int bit)
    {
        int mask = 15 << 4;
        return (bit & mask) >> 4;
    }
    public static int MaskHoleLowRank(int bit)
    {
        int mask = 15;
        return bit & mask;
    }

}