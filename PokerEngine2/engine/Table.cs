using System;
using System.Collections.Generic;
using System.Linq;

public class Table
{
    public int dealerBtn;
    public List<int> blindPos;
    public Seats seats;
    public Deck deck;
    public List<Card> communityCard;

    public Table(Deck cheatDeck)
    {
        dealerBtn = 0;
        blindPos = new List<int>();
        seats = new Seats();
        deck = cheatDeck ?? new Deck();
        communityCard = new List<Card>();
    }

    public void SetBlindPos(int sbPos, int bbPos)
    {
        blindPos = new List<int> { sbPos, bbPos };
    }

    public int SbPos()
    {
        if (blindPos == null) throw new Exception("Blind position is not yet set");
        return blindPos[0];
    }

    public int BbPos()
    {
        if (blindPos == null) throw new Exception("Blind position is not yet set");
        return blindPos[1];
    }

    public List<int> GetCommunityCard()
    {
        return communityCard.Select(card => card.ToId()).ToList();
    }

    public void AddCommunityCard(Card card)
    {
        if (communityCard.Count == 5)
        {
            throw new InvalidOperationException("Community card is already full");
        }
        communityCard.Add(card);
    }

    public void Reset()
    {
        deck.Restore();
        communityCard.Clear();
        foreach (var player in seats.players)
        {
            player.ClearHoleCard();
            player.ClearActionHistories();
            player.ClearPayInfo();
        }
    }

    public void ShiftDealerBtn()
    {
        dealerBtn = NextActivePlayerPos(dealerBtn);
    }

    public int NextActivePlayerPos(int startPos)
    {
        return FindEntitledPlayerPos(startPos, player => player.IsActive() && player.stack != 0);
    }

    public int NextAskWaitingPlayerPos(int startPos)
    {
        return FindEntitledPlayerPos(startPos, player => player.IsWaitingAsk());
    }

    public List<object> Serialize()
    {
        var communityCardIds = communityCard.Select(card => card.ToId()).ToList();
        return new List<object>
        {
            dealerBtn, seats.Serialize(),
            deck.Serialize(), communityCardIds, blindPos
        };
    }

    public static Table Deserialize(List<object> serial)
    {
        var deck = Deck.Deserialize((List<object>)serial[2]);
        var communityCard = ((List<int>)serial[3]).Select(Card.FromId).ToList();
        var table = new Table(deck);
        table.dealerBtn = (int)serial[0];
        table.seats = Seats.Deserialize((List<object>)serial[1]);
        table.communityCard = communityCard;
        table.blindPos = (List<int>)serial[4];
        return table;
    }

    private int FindEntitledPlayerPos(int startPos, Func<Player, bool> checkMethod)
    {
        var players = seats.players;
        var searchTargets = players.Concat(players).Skip(startPos + 1).Take(players.Count).ToList();
        if (searchTargets.Count != players.Count)
        {
            throw new InvalidOperationException("Unexpected error in finding entitled player position");
        }

        var matchPlayer = searchTargets.FirstOrDefault(player => checkMethod(player));
        return matchPlayer == null ? -1 : players.IndexOf(matchPlayer);
    }
}
