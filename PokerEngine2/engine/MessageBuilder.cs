using System.Collections.Generic;
using System.Linq;

public class MessageBuilder
{
    private const string GAME_START_MESSAGE = "game_start_message";
    private const string ROUND_START_MESSAGE = "round_start_message";
    private const string STREET_START_MESSAGE = "street_start_message";
    private const string ASK_MESSAGE = "ask_message";
    private const string GAME_UPDATE_MESSAGE = "game_update_message";
    private const string ROUND_RESULT_MESSAGE = "round_result_message";
    private const string GAME_RESULT_MESSAGE = "game_result_message";

    public static Dictionary<string, object> BuildGameStartMessage(Dictionary<string, object> config, Seats seats)
    {
        var message = new Dictionary<string, object>
        {
            {"message_type", GAME_START_MESSAGE},
            {"game_information", DataEncoder.EncodeGameInformation(config, seats)}
        };
        return BuildNotificationMessage(message);
    }

    public static Dictionary<string, object> BuildRoundStartMessage(int roundCount, int playerPos, Seats seats)
    {
        var player = seats.players[playerPos];
        var holeCard = DataEncoder.EncodePlayer(player, holecard: true)["hole_card"];
        var message = new Dictionary<string, object>
        {
            {"message_type", ROUND_START_MESSAGE},
            {"round_count", roundCount},
            {"hole_card", holeCard}
        };
        message = message.Concat(DataEncoder.EncodeSeats(seats)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return BuildNotificationMessage(message);
    }

    public static Dictionary<string, object> BuildStreetStartMessage(Dictionary<string, object> state)
    {
        var message = new Dictionary<string, object>
        {
            {"message_type", STREET_START_MESSAGE},
            {"round_state", DataEncoder.EncodeRoundState(state)}
        };
        message = message.Concat(DataEncoder.EncodeStreet((int)state["street"])).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return BuildNotificationMessage(message);
    }

    public static Dictionary<string, object> BuildAskMessage(int playerPos, Dictionary<string, object> state)
    {
        var players = ((Table)state["table"]).seats.players;
        var player = players[playerPos];
        var holeCard = DataEncoder.EncodePlayer(player, holecard: true)["hole_card"];
        var validActions = ActionChecker.LegalActions(players, playerPos, (int)state["small_blind_amount"]);
        var message = new Dictionary<string, object>
        {
            {"message_type", ASK_MESSAGE},
            {"hole_card", holeCard},
            {"valid_actions", validActions},
            {"round_state", DataEncoder.EncodeRoundState(state)},
            {"action_histories", DataEncoder.EncodeActionHistories((Table)state["table"])}
        };
        return BuildAskMessage(message);
    }

    public static Dictionary<string, object> BuildGameUpdateMessage(int playerPos, string action, int amount, Dictionary<string, object> state)
    {
        var player = ((Table)state["table"]).seats.players[playerPos];
        var message = new Dictionary<string, object>
        {
            {"message_type", GAME_UPDATE_MESSAGE},
            {"action", DataEncoder.EncodeAction(player, action, amount)},
            {"round_state", DataEncoder.EncodeRoundState(state)},
            {"action_histories", DataEncoder.EncodeActionHistories((Table)state["table"])}
        };
        return BuildNotificationMessage(message);
    }

    public static Dictionary<string, object> BuildRoundResultMessage(int roundCount, List<Player> winners, List<object> handInfo, Dictionary<string, object> state)
    {
        var message = new Dictionary<string, object>
        {
            {"message_type", ROUND_RESULT_MESSAGE},
            {"round_count", roundCount},
            {"hand_info", handInfo},
            {"round_state", DataEncoder.EncodeRoundState(state)}
        };
        message = message.Concat(DataEncoder.EncodeWinners(winners)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return BuildNotificationMessage(message);
    }

    public static Dictionary<string, object> BuildGameResultMessage(Dictionary<string, object> config, Seats seats)
    {
        var message = new Dictionary<string, object>
        {
            {"message_type", GAME_RESULT_MESSAGE},
            {"game_information", DataEncoder.EncodeGameInformation(config, seats)}
        };
        return BuildNotificationMessage(message);
    }

    private static Dictionary<string, object> BuildAskMessage(Dictionary<string, object> message)
    {
        return new Dictionary<string, object>
        {
            {"type", "ask"},
            {"message", message}
        };
    }

    public static Dictionary<string, object> BuildNotificationMessage(Dictionary<string, object> message)
    {
        return new Dictionary<string, object>
        {
            {"type", "notification"},
            {"message", message}
        };
    }
}
