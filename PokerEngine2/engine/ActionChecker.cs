using System;
using System.Collections.Generic;
using System.Linq;

public class ActionChecker
{
    public static (string action, int? amount) CorrectAction(List<Player> players, int playerPos, int sbAmount, string action, int? amount = null)
    {
        if (IsAllin(players[playerPos], action, amount))
        {
            amount = players[playerPos].stack + players[playerPos].PaidSum();
        }
        else if (IsIllegal(players, playerPos, sbAmount, action, amount))
        {
            action = "fold";
            amount = 0;
        }
        return (action, amount);
    }

    public static bool IsAllin(Player player, string action, int? betAmount)
    {
        if (action == "call")
        {
            return betAmount >= player.stack + player.PaidSum();
        }
        else if (action == "raise")
        {
            return betAmount == player.stack + player.PaidSum();
        }
        else
        {
            return false;
        }
    }

    public static int NeedAmountForAction(Player player, int amount)
    {
        return amount - player.PaidSum();
    }

    public static int AgreeAmount(List<Player> players)
    {
        var lastRaise = FetchLastRaise(players);
        return lastRaise != null && lastRaise.ContainsKey("amount") ? (int)lastRaise["amount"] : 0;
    }

    public static List<Dictionary<string, object>> LegalActions(List<Player> players, int playerPos, int sbAmount)
    {
        var minRaise = MinRaiseAmount(players, sbAmount);
        var maxRaise = players[playerPos].stack + players[playerPos].PaidSum();
        if (maxRaise < minRaise)
        {
            minRaise = maxRaise = -1;
        }

        return new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "action", "fold" }, { "amount", 0 } },
            new Dictionary<string, object> { { "action", "call" }, { "amount", AgreeAmount(players) } },
            new Dictionary<string, object> { { "action", "raise" }, { "amount", new { min = minRaise, max = maxRaise } } }
        };
    }

    public static bool IsLegal(List<Player> players, int playerPos, int sbAmount, string action, int? amount = null)
    {
        return !IsIllegal(players, playerPos, sbAmount, action, amount);
    }

    private static bool IsIllegal(List<Player> players, int playerPos, int sbAmount, string action, int? amount = null)
    {
        if (action == "fold")
        {
            return false;
        }
        else if (action == "call")
        {
            return _isShortOfMoney(players[playerPos], amount) || _isIllegalCall(players, amount);
        }
        else if (action == "raise")
        {
            return _isShortOfMoney(players[playerPos], amount) || _isIllegalRaise(players, amount, sbAmount);
        }
        return false;
    }

    private static bool _isIllegalCall(List<Player> players, int? amount)
    {
        return amount != AgreeAmount(players);
    }

    private static bool _isIllegalRaise(List<Player> players, int? amount, int sbAmount)
    {
        return MinRaiseAmount(players, sbAmount) > amount;
    }

    private static int MinRaiseAmount(List<Player> players, int sbAmount)
    {
        var raise = FetchLastRaise(players);
        return raise != null && raise.ContainsKey("amount") && raise.ContainsKey("add_amount")
            ? (int)raise["amount"] + (int)raise["add_amount"]
            : sbAmount * 2;
    }

    private static bool _isShortOfMoney(Player player, int? amount)
    {
        return player.stack < amount - player.PaidSum();
    }

    private static Dictionary<string, object> FetchLastRaise(List<Player> players)
    {
        var allHistories = players.SelectMany(p => p.actionHistories).ToList();
        var raiseHistories = allHistories.Where(h => h["action"].ToString() == "RAISE" || h["action"].ToString() == "SMALLBLIND" || h["action"].ToString() == "BIGBLIND").ToList();
        return raiseHistories.Count == 0 ? null : raiseHistories.MaxBy(h => (int)h["amount"]);
    }
}
