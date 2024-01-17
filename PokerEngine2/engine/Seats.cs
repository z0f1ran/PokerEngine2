using System.Collections.Generic;

public class Seats
{
    public List<Player> players;

    public Seats()
    {
        players = new List<Player>();
    }

    public void Sitdown(Player player)
    {
        players.Add(player);
    }

    public int Size()
    {
        return players.Count;
    }

    public int CountActivePlayers()
    {
        return players.Count(p => p.IsActive());
    }

    public int CountAskWaitPlayers()
    {
        return players.Count(p => p.IsWaitingAsk());
    }

    public List<object> Serialize()
    {
        return players.SelectMany(player => player.Serialize()).ToList();
    }

    public static Seats Deserialize(List<object> serial)
    {
        Seats seats = new Seats();
        seats.players = serial.ConvertAll(s => Player.Deserialize((List<object>)s));
        return seats;
    }
}
