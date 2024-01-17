using System;
using System.Collections.Generic;
using System.Linq;

public class GameEvaluator
{
    public static Tuple<List<Player>, List<object>, Dictionary<int, int>> Judge(Table table)
    {
        List<Player> winners = FindWinnersFrom(table.communityCard, table.seats.players);
        List<object> handInfo = GenHandInfoIfNeeded(table.seats.players, table.communityCard);
        Dictionary<int, int> prizeMap = CalcPrizeDistribution(table.communityCard, table.seats.players);
        return Tuple.Create(winners, handInfo, prizeMap);
    }

    public static List<object> CreatePot(List<Player> players)
    {
        List<Dictionary<string, object>> sidePots = GetSidePots(players);
        Dictionary<string, object> mainPot = GetMainPot(players, sidePots);
        return sidePots.Cast<object>().ToList().Concat(new List<object> { mainPot }).ToList();
    }

    private static Dictionary<int, int> CalcPrizeDistribution(List<Card> communityCard, List<Player> players)
    {
        Dictionary<int, int> prizeMap = CreatePrizeMap(players.Count);
        List<Dictionary<string, object>> pots = CreatePot(players).Cast<Dictionary<string, object>>().ToList();

        foreach (var pot in pots)
        {
            List<Player> winners = FindWinnersFrom(communityCard, (List<Player>)pot["eligibles"]);
            int prize = (int)pot["amount"] / winners.Count;

            foreach (var winner in winners)
            {
                prizeMap[players.IndexOf(winner)] += prize;
            }
        }

        return prizeMap;
    }

    private static Dictionary<int, int> CreatePrizeMap(int playerNum)
    {
        Dictionary<int, int> prizeMap = new Dictionary<int, int>();

        for (int i = 0; i < playerNum; i++)
        {
            prizeMap[i] = 0;
        }

        return prizeMap;
    }

    private static List<Player> FindWinnersFrom(List<Card> communityCard, List<Player> players)
    {
        Func<Player, int> scorePlayer = (player) => HandEvaluator.EvalHand(player.holeCard, communityCard);

        List<Player> activePlayers = players.Where(player => player.IsActive()).ToList();
        List<int> scores = activePlayers.Select(scorePlayer).ToList();
        int bestScore = scores.Max();
        List<Tuple<int, Player>> scoreWithPlayers = scores.Zip(activePlayers, Tuple.Create).ToList();
        List<Player> winners = scoreWithPlayers.Where(s_p => s_p.Item1 == bestScore).Select(s_p => s_p.Item2).ToList();

        return winners;
    }

    private static List<object> GenHandInfoIfNeeded(List<Player> players, List<Card> community)
    {
        List<Player> activePlayers = players.Where(player => player.IsActive()).ToList();
        Func<Player, Dictionary<string, object>> genHandInfo = (player) => new Dictionary<string, object>
        {
            { "uuid", player.uuid },
            { "hand", HandEvaluator.GenHandRankInfo(player.holeCard, community) }
        };

        return activePlayers.Count == 1 ? new List<object>() : activePlayers.Select(genHandInfo).Cast<object>().ToList();
    }

    private static Dictionary<string, object> GetMainPot(List<Player> players, List<Dictionary<string, object>> sidePots)
    {
        int maxPay = GetMaxPay(players);
        Dictionary<string, object> mainPot = new Dictionary<string, object>
        {
            { "amount", GetPlayersPaySum(players) - GetSidePotsSum(sidePots) },
            { "eligibles", players.Where(player => player.payInfo.amount == maxPay).ToList() }
        };
        return mainPot;
    }
    private static int GetMaxPay(List<Player> players)
    {
        return players.Max(player => player.payInfo.amount);
    }

    private static int GetPlayersPaySum(List<Player> players)
    {
        return players.Sum(player => player.payInfo.amount);
    }

    private static List<Dictionary<string, object>> GetSidePots(List<Player> players)
    {
        List<int> payAmounts = FetchAllinPayinfo(players).Select(payInfo => payInfo.amount).ToList();
        Func<List<Dictionary<string, object>>, int, List<Dictionary<string, object>>> genSidePots = (sidepots, allinAmount) =>
            sidepots.Concat(new List<Dictionary<string, object>> { CreateSidePot(players, sidepots, allinAmount) }).ToList();

        return payAmounts.Aggregate(new List<Dictionary<string, object>>(), genSidePots);
    }

    private static Dictionary<string, object> CreateSidePot(List<Player> players, List<Dictionary<string, object>> smallerSidePots, int allinAmount)
    {
        return new Dictionary<string, object>
        {
            { "amount", CalcSidePotSize(players, smallerSidePots, allinAmount) },
            { "eligibles", SelectEligibles(players, allinAmount) }
        };
    }

    private static int CalcSidePotSize(List<Player> players, List<Dictionary<string, object>> smallerSidePots, int allinAmount)
    {
        Func<int, Player, int> addChipForPot = (pot, player) => pot + Math.Min(allinAmount, player.payInfo.amount);
        int targetPotSize = players.Aggregate(0, addChipForPot);
        return targetPotSize - GetSidePotsSum(smallerSidePots);
    }

    private static int GetSidePotsSum(List<Dictionary<string, object>> sidepots)
    {
        return sidepots.Aggregate(0, (sum, sidepot) => sum + (int)sidepot["amount"]);
    }

    private static List<Player> SelectEligibles(List<Player> players, int allinAmount)
    {
        return players.Where(player => IsEligible(player, allinAmount)).ToList();
    }

    private static bool IsEligible(Player player, int allinAmount)
    {
        return player.payInfo.amount >= allinAmount && player.payInfo.status != PayInfo.FOLDED;
    }

    private static List<PayInfo> FetchAllinPayinfo(List<Player> players)
    {
        return GetPayInfo(players).Where(info => info.status == PayInfo.ALLIN).OrderBy(info => info.amount).ToList();
    }

    private static List<PayInfo> GetPayInfo(List<Player> players)
    {
        return players.Select(player => player.payInfo).ToList();
    }
}
