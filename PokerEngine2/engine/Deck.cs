using System;
using System.Collections.Generic;
using System.Linq;

public class Deck
{
    private bool cheat;
    private List<int> cheatCardIds;
    private List<Card> deck;

    public Deck(List<int> deckIds = null, bool cheat = false, List<int> cheatCardIds = null)
    {
        this.cheat = cheat;
        this.cheatCardIds = cheatCardIds ?? new List<int>();
        this.deck = deckIds?.Select(Card.FromId).ToList() ?? Setup();
    }

    public Card DrawCard()
    {
        Card drawnCard = deck.Last();
        deck.RemoveAt(deck.Count - 1);
        return drawnCard;
    }

    public List<Card> DrawCards(int num)
    {
        return Enumerable.Range(0, num).Select(_ => DrawCard()).ToList();
    }

    public int Size()
    {
        return deck.Count;
    }

    public void Restore()
    {
        deck = Setup();
    }

    public void Shuffle()
    {
        if (!cheat)
        {
            Random random = new Random();
            deck = deck.OrderBy(card => random.Next()).ToList();
        }
    }

    // Serialize format: [cheatFlag, cheatCardIds, deckCardIds]
    public List<object> Serialize()
    {
        return new List<object> { cheat, cheatCardIds, deck.Select(card => card.ToId()).ToList() };
    }

    public static Deck Deserialize(List<object> serial)
    {
        bool cheat = (bool)serial[0];
        List<int> cheatCardIds = (List<int>)serial[1];
        List<int> deckIds = (List<int>)serial[2];
        return new Deck(deckIds, cheat, cheatCardIds);
    }

    private List<Card> Setup()
    {
        return cheat ? SetupCheatDeck() : Setup52Cards();
    }

    private List<Card> Setup52Cards()
    {
        return Enumerable.Range(1, 52).Select(Card.FromId).ToList();
    }

    private List<Card> SetupCheatDeck()
    {
        return cheatCardIds.Select(Card.FromId).Reverse().ToList();
    }
}
