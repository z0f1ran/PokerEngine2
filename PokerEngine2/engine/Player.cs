using System;
using System.Collections.Generic;

public class Player
{
    public const string ActionFoldStr = "FOLD";
    public const string ActionCallStr = "CALL";
    public const string ActionRaiseStr = "RAISE";
    public const string ActionSmallBlind = "SMALLBLIND";
    public const string ActionBigBlind = "BIGBLIND";
    public const string ActionAnte = "ANTE";

    public string name;
    public string uuid;
    public List<Card> holeCard;
    public int stack;
    public List<List<Dictionary<string, object>>> roundActionHistories;
    public List<Dictionary<string, object>> actionHistories;
    public PayInfo payInfo;

    public Player(string uuid, int initialStack, string name = "No Name")
    {
        this.name = name;
        this.uuid = uuid;
        this.holeCard = new List<Card>();
        this.stack = initialStack;
        this.roundActionHistories = InitRoundActionHistories();
        this.actionHistories = new List<Dictionary<string, object>>();
        this.payInfo = new PayInfo();
    }

    public void AddHoleCard(List<Card> cards)
    {
        if (holeCard.Count != 0)
        {
            throw new InvalidOperationException("Hole card is already set");
        }
        if (cards.Count != 2)
        {
            throw new InvalidOperationException($"You passed {cards.Count} hole cards");
        }
        if (!cards.TrueForAll(card => card is Card))
        {
            throw new InvalidOperationException("You passed not Card object as hole card");
        }
        holeCard = cards;
    }

    public void ClearHoleCard()
    {
        holeCard.Clear();
    }

    public void AppendChip(int amount)
    {
        stack += amount;
    }

    public void CollectBet(int amount)
    {
        if (stack < amount)
        {
            throw new InvalidOperationException($"Failed to collect {amount} chips. Because he has only {stack} chips");
        }
        stack -= amount;
    }

    public bool IsActive()
    {
        return payInfo.status != PayInfo.FOLDED;
    }

    public bool IsWaitingAsk()
    {
        return payInfo.status == PayInfo.PAY_TILL_END;
    }

    public void AddActionHistory(int kind, int chipAmount = 0, int addAmount = 0, int sbAmount = 0)
    {
        Dictionary<string, object> history;
        if (kind == PokerConstants.Action.FOLD)
        {
            history = FoldHistory();
        }
        else if (kind == PokerConstants.Action.CALL)
        {
            history = CallHistory(chipAmount);
        }
        else if (kind == PokerConstants.Action.RAISE)
        {
            history = RaiseHistory(chipAmount, addAmount);
        }
        else if (kind == PokerConstants.Action.SMALL_BLIND)
        {
            history = BlindHistory(true, sbAmount);
        }
        else if (kind == PokerConstants.Action.BIG_BLIND)
        {
            history = BlindHistory(false, sbAmount);
        }
        else if (kind == PokerConstants.Action.ANTE)
        {
            history = AnteHistory(chipAmount);
        }
        else
        {
            throw new InvalidOperationException($"Unknown action history is added (kind = {kind})");
        }
        history = AddUuidOnHistory(history);
        actionHistories.Add(history);
    }

    public void SaveStreetActionHistories(int streetFlag)
    {
        roundActionHistories[streetFlag] = new List<Dictionary<string, object>>(actionHistories);
        actionHistories = new List<Dictionary<string, object>>();
    }

    public void ClearActionHistories()
    {
        roundActionHistories = InitRoundActionHistories();
        actionHistories = new List<Dictionary<string, object>>();
    }

    public void ClearPayInfo()
    {
        payInfo = new PayInfo();
    }

    public int PaidSum()
    {
        List<Dictionary<string, object>> payHistory = actionHistories.FindAll(h => !h["action"].Equals("FOLD") && !h["action"].Equals("ANTE"));
        Dictionary<string, object> lastPayHistory = payHistory.Count != 0 ? payHistory[payHistory.Count - 1] : null;
        return lastPayHistory != null ? Convert.ToInt32(lastPayHistory["amount"]) : 0;
    }

    public List<object> Serialize()
    {
        List<int> hole = holeCard.ConvertAll(card => card.ToId());
        return new List<object>
        {
            name, uuid, stack, hole,
            new List<object>(actionHistories),
            payInfo.Serialize(),
            new List<object>(roundActionHistories)
        };
    }

    public static Player Deserialize(List<object> serial)
    {
        List<Card> hole = ((List<int>)serial[3]).ConvertAll(Card.FromId);
        Player player = new Player((string)serial[1], Convert.ToInt32(serial[2]), (string)serial[0]);
        if (hole.Count != 0) player.AddHoleCard(hole);

        player.actionHistories = ((List<object>)serial[4]).ConvertAll(history => (Dictionary<string, object>)history);

        player.payInfo = PayInfo.Deserialize((List<object>)serial[5]);

        player.roundActionHistories = ((List<List<object>>)serial[6]).ConvertAll(
            roundHistory => roundHistory.ConvertAll(
                history => (Dictionary<string, object>)history
            )
        );

        return player;
    }
    private List<List<Dictionary<string, object>>> InitRoundActionHistories()
    {
        return new List<List<Dictionary<string, object>>>(new List<Dictionary<string, object>>[4]);
    }

    private Dictionary<string, object> FoldHistory()
    {
        return new Dictionary<string, object> { { "action", ActionFoldStr } };
    }

    private Dictionary<string, object> CallHistory(int betAmount)
    {
        return new Dictionary<string, object>
        {
            {"action", ActionCallStr},
            {"amount", betAmount},
            {"paid", betAmount - PaidSum()}
        };
    }

    private Dictionary<string, object> RaiseHistory(int betAmount, int addAmount)
    {
        return new Dictionary<string, object>
        {
            {"action", ActionRaiseStr},
            {"amount", betAmount},
            {"paid", betAmount - PaidSum()},
            {"add_amount", addAmount}
        };
    }

    private Dictionary<string, object> BlindHistory(bool smallBlind, int sbAmount)
    {
        if (sbAmount == 0)
        {
            throw new InvalidOperationException("Small Blind amount must be greater than 0.");
        }

        string action = smallBlind ? ActionSmallBlind : ActionBigBlind;
        int amount = smallBlind ? sbAmount : sbAmount * 2;
        int addAmount = sbAmount;

        return new Dictionary<string, object>
        {
            {"action", action},
            {"amount", amount},
            {"add_amount", addAmount}
        };
    }

    private Dictionary<string, object> AnteHistory(int payAmount)
    {
        if (payAmount <= 0)
        {
            throw new InvalidOperationException("Ante amount must be greater than 0.");
        }

        return new Dictionary<string, object>
        {
            {"action", ActionAnte},
            {"amount", payAmount}
        };
    }

    private Dictionary<string, object> AddUuidOnHistory(Dictionary<string, object> history)
    {
        history["uuid"] = uuid;
        return history;
    }
}
