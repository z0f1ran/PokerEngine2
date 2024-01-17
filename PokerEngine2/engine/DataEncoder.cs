using System;
using System.Collections.Generic;
using System.Linq;
public class DataEncoder
{
    public const string PAY_INFO_PAY_TILL_END_STR = "participating";
    public const string PAY_INFO_ALLIN_STR = "allin";
    public const string PAY_INFO_FOLDED_STR = "folded";

    public static Dictionary<string, object> EncodePlayer(Player player, bool holecard = false)
    {
        string payStatusStr = PayinfoToStr(player.payInfo.status);
        var encodedPlayer = new Dictionary<string, object>
        {
            {"name", player.name},
            {"uuid", player.uuid},
            {"stack", player.stack},
            {"state", payStatusStr}
        };
        if (holecard)
        {
            encodedPlayer["hole_card"] = player.holeCard.Select(card => card.ToString()).ToList();
        }
        return encodedPlayer;
    }

    public static Dictionary<string, object> EncodeSeats(Seats seats)
    {
        return new Dictionary<string, object>
        {
            {"seats", seats.players.Select((player, index) => EncodePlayer(player)).ToList()}
        };
    }

    public static Dictionary<string, object> EncodePot(List<Player> players)
    {
        var pots = GameEvaluator.CreatePot(players);
        var main = new Dictionary<string, object> { { "amount", ((Dictionary<string, object>)pots[0])["amount"] } };
        var side = pots.Skip(1)
            .Select(pot => new Dictionary<string, object>
            {
                {"amount", ((Dictionary<string, object>)pot)["amount"]},
                {"eligibles", ((List<Player>)((Dictionary<string, object>)pot)["eligibles"]).Select(p => p.uuid).ToList()}
            }).ToList();
        return new Dictionary<string, object> { { "main", main }, { "side", side } };
    }

    public static Dictionary<string, object> EncodeGameInformation(Dictionary<string, object> config, Seats seats)
    {
        var encodedSeats = EncodeSeats(seats);
        var gameInfo = new Dictionary<string, object>
    {
        {"player_num", seats.players.Count},
        {"rule", config}
    };

        foreach (var item in encodedSeats)
        {
            gameInfo.Add(item.Key, item.Value);
        }

        return gameInfo;
    }

    public static Dictionary<string, object> EncodeValidActions(int callAmount, int minBetAmount, int maxBetAmount)
    {
        return new Dictionary<string, object>
        {
            {
                "valid_actions", new List<object>
                {
                    new Dictionary<string, object> {{"action", "fold"}, {"amount", 0}},
                    new Dictionary<string, object> {{"action", "call"}, {"amount", callAmount}},
                    new Dictionary<string, object>
                    {
                        {"action", "raise"},
                        {"amount", new Dictionary<string, int> {{"min", minBetAmount}, {"max", maxBetAmount}}}
                    }
                }
            }
        };
    }





    public static Dictionary<string, object> EncodeAction(Player player, string action, int amount)
    {
        return new Dictionary<string, object>
        {
            {"player_uuid", player.uuid},
            {"action", action},
            {"amount", amount}
        };
    }

    public static Dictionary<string, object> EncodeStreet(int street)
    {
        string streetName;
        switch (street)
        {
            case PokerConstants.Street.PREFLOP:
                streetName = "preflop";
                break;
            case PokerConstants.Street.FLOP:
                streetName = "flop";
                break;
            case PokerConstants.Street.TURN:
                streetName = "turn";
                break;
            case PokerConstants.Street.RIVER:
                streetName = "river";
                break;
            case PokerConstants.Street.SHOWDOWN:
                streetName = "showdown";
                break;
            default:
                throw new ArgumentException("Invalid street value");
        }

        return new Dictionary<string, object>
    {
        {"street", streetName}
    };
    }


    public static Dictionary<string, object> EncodeActionHistories(Table table)
    {
        var allStreetHistories = new List<List<List<Dictionary<string, object>>>>();
        for (int street = 0; street < 4; street++)
        {
            var streetHistories = table.seats.players.Select(player => player.roundActionHistories[street])
                                                    .Where(history => history != null)
                                                    .Select(history => new List<Dictionary<string, object>>(history))
                                                    .ToList();
            allStreetHistories.Add(streetHistories);
        }

        var currentStreetHistories = table.seats.players.Select(player => player.actionHistories)
                                                         .Select(history => new List<Dictionary<string, object>>(history))
                                                         .ToList();
        allStreetHistories.Add(currentStreetHistories);

        var streetNames = new[] { "preflop", "flop", "turn", "river" };
        var actionHistories = new Dictionary<string, List<List<Dictionary<string, object>>>>();

        for (int i = 0; i < streetNames.Length; i++)
        {
            var streetName = streetNames[i];
            var histories = OrderHistories(table.SbPos(), allStreetHistories[i]);
            actionHistories[streetName] = histories;
        }

        return new Dictionary<string, object> { { "action_histories", actionHistories } };
    }


    public static Dictionary<string, object> EncodeWinners(List<Player> winners)
    {
        return new Dictionary<string, object>
        {
            {"winners", EncodePlayers(winners)}
        };
    }

    public static Dictionary<string, object> EncodeRoundState(Dictionary<string, object> state)
    {
        var table = (Table)state["table"];

        var encodedSeats = EncodeSeats(table.seats);
        var encodedActionHistories = EncodeActionHistories(table);
        var pot = EncodePot(table.seats.players);

        var roundState = new Dictionary<string, object>
    {
        {"street", GetStreetName((int)state["street"])}, // Assuming you have a method to get street name
        {"pot", pot},
        {"community_card", table.GetCommunityCard().Select(card => card.ToString()).ToList()},
        {"next_player", state["next_player"]},
        {"small_blind_pos", table.SbPos()},
        {"big_blind_pos", table.BbPos()},
        {"round_count", state["round_count"]},
        {"small_blind_amount", state["small_blind_amount"]}
    };

        foreach (var kvp in encodedSeats)
        {
            roundState[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in encodedActionHistories)
        {
            roundState[kvp.Key] = kvp.Value;
        }

        return roundState;
    }

    private static string GetStreetName(int street)
    {
        switch (street)
        {
            case 0:
                return "preflop";
            case 1:
                return "flop";
            case 2:
                return "turn";
            case 3:
                return "river";
            case 4:
                return "showdown";
            default:
                return "unknown"; // Добавьте логику для других значений, если необходимо
        }
    }

    private static string PayinfoToStr(int status)
    {
        if (status == PayInfo.PAY_TILL_END)
        {
            return PAY_INFO_PAY_TILL_END_STR;
        }
        if (status == PayInfo.ALLIN)
        {
            return PAY_INFO_ALLIN_STR;
        }
        if (status == PayInfo.FOLDED)
        {
            return PAY_INFO_FOLDED_STR;
        }
        return "";
    }

    private static List<List<Dictionary<string, object>>> OrderHistories(int startPos, List<List<Dictionary<string, object>>> playerHistories)
    {
        var orderedPlayerHistories = playerHistories
            .Select(histories => histories.Skip(startPos).Concat(histories.Take(startPos)).ToList())
            .ToList();

        var maxLen = orderedPlayerHistories.Max(histories => histories.Count);
        var unifiedHistories = orderedPlayerHistories
            .Select(histories => histories.Concat(Enumerable.Repeat(new Dictionary<string, object>(), maxLen - histories.Count)).ToList())
            .ToList();

        return unifiedHistories
            .Select(histories => histories.Select(item => new Dictionary<string, object>(item)).ToList())
            .ToList();
    }

    private static List<Dictionary<string, object>> UnifyLength(int maxLen, List<Dictionary<string, object>> lst)
    {
        for (int i = 0; i < maxLen - lst.Count; i++)
        {
            lst.Add(null);
        }
        return lst;
    }

    private static List<Dictionary<string, object>> EncodePlayers(List<Player> players)
    {
        return players.Select(player => EncodePlayer(player)).ToList();
    }

}
