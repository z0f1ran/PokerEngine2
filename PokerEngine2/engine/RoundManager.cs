using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerEngine2.engine
{
    internal class RoundManager
    {
        private static Tuple<Dictionary<string, object>, Tuple<int,Dictionary<string, object>>> ForwardStreet(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            var streetStartMsg = Tuple.Create(-1, MessageBuilder.BuildStreetStartMessage(state));

            if (table.seats.CountActivePlayers() == 1)
            {
                streetStartMsg = null;
            }

            if (table.seats.CountAskWaitPlayers() <= 1)
            {
                state["street"] = (int)state["street"] + 1;
                var result = StartStreet(state);
                return result;
            }
            else
            {
                var nextPlayerPos = (int)state["next_player"];
                var nextPlayer = table.seats.players[nextPlayerPos];
                var askMessage = Tuple.Create(nextPlayer.uuid, MessageBuilder.BuildAskMessage(nextPlayerPos, state));
                return Tuple.Create(state, streetStartMsg.Concat(askMessage));
            }
        }

        private static Tuple<Dictionary<string, object>, Tuple<int, Dictionary<string, object>>> StartStreet(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            var nextPlayerPos = table.NextAskWaitingPlayerPos(table.SbPos() - 1);
            state["next_player"] = nextPlayerPos;
            var street = (int)state["street"];

            switch (street)
            {
                case PokerConstants.Street.PREFLOP:
                    return Preflop(state);
                case PokerConstants.Street.FLOP:
                    return Flop(state);
                case PokerConstants.Street.TURN:
                    return Turn(state);
                case PokerConstants.Street.RIVER:
                    return River(state);
                case PokerConstants.Street.SHOWDOWN:
                    return Showdown(state);
                default:
                    throw new ArgumentException($"Street is already finished [street = {street}]");
            }
        }

        private static Tuple<Dictionary<string, object>, Dictionary<string, object>> Preflop(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            for (int i = 0; i < 2; i++)
            {
                state["next_player"] = table.NextAskWaitingPlayerPos((int)state["next_player"]);
            }

            return ForwardStreet(state);
        }

        private static Tuple<Dictionary<string, object>, Dictionary<string, object>> Flop(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            var drawnCards = table.deck.DrawCards(3);

            foreach (var card in drawnCards)
            {
                table.AddCommunityCard(card);
            }

            return ForwardStreet(state);
        }

        private static Tuple<Dictionary<string, object>, Dictionary<string, object>> Turn(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            var drawnCard = table.deck.DrawCard();
            table.AddCommunityCard(drawnCard);

            return ForwardStreet(state);
        }

        private static Tuple<Dictionary<string, object>, Dictionary<string, object>> River(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            table.AddCommunityCard(table.deck.DrawCard());

            return ForwardStreet(state);
        }

        private static Tuple<Dictionary<string,object>, Dictionary<string,object>> Showdown(Dictionary<string, object> state)
        {
            var table = (Table)state["table"];
            var (winners, handInfo, prizeMap) = GameEvaluator.Judge(table);
            PrizeToWinners(table.seats.players, prizeMap);
            var resultMessage = MessageBuilder.BuildRoundResultMessage((int)state["round_count"], winners, handInfo, state);
            table.Reset();
            state["street"] = (int)state["street"] + 1;

            return Tuple.Create(state, resultMessage);
        }

        private static void PrizeToWinners(List<Player> players, Dictionary<int, int> prizeMap)
        {
            foreach (var entry in prizeMap)
            {
                int idx = entry.Key;
                int prize = entry.Value;
                players[idx].AppendChip(prize);
            }
        }

        private static Dictionary<string, object> GenerateMessage(int roundCount, int playerPos, Seats seats)
        {
            var player = seats.players[playerPos];
            var holeCard = DataEncoder.EncodePlayer(player, holecard: true)["hole_card"];

            var message = new Dictionary<string, object>
        {
            {"message_type", "ROUND_START_MESSAGE"},
            {"round_count", roundCount},
            {"hole_card", holeCard}
        };

            // Добавляем данные из EncodeSeats с использованием LINQ
            message = message.Concat(DataEncoder.EncodeSeats(seats)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return MessageBuilder.BuildNotificationMessage(message);
        }

        private static List<Dictionary<string, object>> RoundStartMessages(int roundCount, Table table)
        {
            List<Player> players = table.seats.players;

            List<Dictionary<string, object>> messages = new List<Dictionary<string, object>>();

            // Используем LINQ для создания сообщений для каждого игрока
            messages = Enumerable.Range(0, players.Count)
                                 .Select(index => GenerateMessage(roundCount, index, table.seats))
                                 .ToList();

            return messages;
        }

        private static Dictionary<string, object> UpdateStateByAction(Dictionary<string, object> state, string action, int betAmount)
        {
            var table = (Table)state["table"];
            var correctedAction = ActionChecker.CorrectAction(table.seats.players, (int)state["next_player"], (int)state["small_blind_amount"], action, betAmount);
            var nextPlayer = table.seats.players[(int)state["next_player"]];

            if (ActionChecker.IsAllin(nextPlayer, correctedAction.action, betAmount))
            {
                nextPlayer.payInfo.UpdateToAllIn();
            }

            AcceptAction(state, correctedAction.action, betAmount);
            return state;
        }

        private static void AcceptAction(Dictionary<string, object> state, string action, int betAmount)
        {
            var player = ((Table)state["table"]).seats.players[(int)state["next_player"]];
            if (action == "call")
            {
                ChipTransaction(player, betAmount);
                player.AddActionHistory(PokerConstants.Action.CALL, betAmount);
            }
            else if (action == "raise")
            {
                ChipTransaction(player, betAmount);
                int addAmount = betAmount - ActionChecker.AgreeAmount(((Table)state["table"]).seats.players);
                player.AddActionHistory(PokerConstants.Action.RAISE, betAmount, addAmount);
            }
            else if (action == "fold")
            {
                player.AddActionHistory(PokerConstants.Action.FOLD);
                player.payInfo.UpdateToFold();
            }
            else
            {
                throw new ArgumentException($"Unexpected action {action} received");
            }
        }



        private static void ChipTransaction(Player player, int betAmount)
        {
            int needAmount = ActionChecker.NeedAmountForAction(player, betAmount);
            player.CollectBet(needAmount);
            player.payInfo.UpdateByPay(needAmount);
        }


        private static KeyValuePair<int, Dictionary<string, object>> UpdateMessage(Dictionary<string, object> state, string action, int betAmount)
        {
            int nextPlayerPos = (int)state["next_player"];
            var message = MessageBuilder.BuildGameUpdateMessage(nextPlayerPos, action, betAmount, state);
            return new KeyValuePair<int, Dictionary<string, object>>(-1, message);
        }

        private static bool IsEveryoneAgreed(Dictionary<string, object> state)
        {
            AgreeLogicBugCatch(state);
            var players = ((Table)state["table"]).seats.players;
            var nextPlayerPos = ((Table)state["table"]).NextAskWaitingPlayerPos((int)state["next_player"]);
            var nextPlayer = !nextPlayerPos.Equals(-1) ? players[Convert.ToInt32(nextPlayerPos)] : null;
            var maxPay = players.Max(p => p.PaidSum());

            var everyoneAgreed = players.Count == players.Count(p => IsAgreed(maxPay, p));
            var lonelyPlayer = ((Table)state["table"]).seats.CountActivePlayers() == 1;
            var noNeedToAsk = ((Table)state["table"]).seats.CountAskWaitPlayers() == 1 &&
                              nextPlayer != null && nextPlayer.IsWaitingAsk() && nextPlayer.PaidSum() == maxPay;

            return everyoneAgreed || lonelyPlayer || noNeedToAsk;
        }

        private static void AgreeLogicBugCatch(Dictionary<string, object> state)
        {
            if (state["table"] is Table table && table.seats.CountActivePlayers() == 0)
            {
                throw new Exception("[__is_everyone_agreed] no-active-players!!");
            }
        }

        private static bool IsAgreed(int maxPay, Player player)
        {
            // BigBlind should be asked action at least once
            bool isPreflop = player.roundActionHistories[0] == null;
            bool bbAskOnce = player.actionHistories.Count == 1
                            && player.actionHistories[0]["action"] == Player.ActionBigBlind;
            bool bbAskCheck = !isPreflop || !bbAskOnce;
            return (bbAskCheck && player.PaidSum() == maxPay && player.actionHistories.Count != 0)
                    || new List<int> { PayInfo.FOLDED, PayInfo.ALLIN }.Contains(player.payInfo.status);
        }


        private static Dictionary<string, object> GenInitialState(int roundCount, int smallBlindAmount, Table table)
        {
            return new Dictionary<string, object>
            {
                { "round_count", roundCount },
                { "small_blind_amount", smallBlindAmount },
                { "street", PokerConstants.Street.PREFLOP },
                { "next_player", table.NextAskWaitingPlayerPos(table.BbPos()) },
                { "table", table }
            };
        }


        private static Dictionary<string, object> DeepCopyState(Dictionary<string, object> state)
        {
            List<object> serializedTable = (List<object>)state["table"];
            Table tableDeepCopy = Table.Deserialize(serializedTable);
            return new Dictionary<string, object>
            {
                { "round_count", state["round_count"] },
                { "small_blind_amount", state["small_blind_amount"] },
                { "street", state["street"] },
                { "next_player", state["next_player"] },
                { "table", tableDeepCopy }
            };
        }

    }
}
