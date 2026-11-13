using System;

namespace DefenseDuell.Progression
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int Schema = 1, Medals, TowerMask = 31;
        public long MatchCounter, ActiveCounter, SettledCounter;
        public string ActiveTicket = "", SettledTicket = "";
        public PlayerProfile Copy() => (PlayerProfile)MemberwiseClone();
    }

    public enum ProfileChange { Applied, AlreadyOwned, InsufficientMedals, InvalidTower, WrongMatch, AlreadySettled, InvalidOutcome }
    public enum ProfileOutcome { Win, Loss, Draw, Aborted }

    /// <summary>Local progression. It is not an online ranking or tamper-proof account balance.</summary>
    public static class ProfileRules
    {
        public const int WinMedals = 100, MaxMedals = 1000000;
        public static int Price(int kind) => kind == 5 ? 200 : kind == 6 ? 300 : -1;
        public static bool Owns(PlayerProfile p, int kind) => kind >= 0 && kind < 7 && (p.TowerMask & (1 << kind)) != 0;
        public static void Validate(PlayerProfile p)
        {
            if (p == null || p.Schema != 1 || p.Medals < 0 || p.Medals > MaxMedals || (p.TowerMask & 31) != 31 || (p.TowerMask & ~127) != 0
                || p.MatchCounter < 0 || p.ActiveCounter < 0 || p.SettledCounter < 0 || p.ActiveCounter > p.MatchCounter || p.SettledCounter > p.MatchCounter
                || p.ActiveTicket == null || p.SettledTicket == null || (p.ActiveTicket.Length == 0) != (p.ActiveCounter == 0)
                || (p.SettledTicket.Length == 0) != (p.SettledCounter == 0)) throw new ArgumentException("Invalid local progression profile.");
            if (p.ActiveCounter != 0 && (p.ActiveCounter != p.MatchCounter || p.ActiveCounter <= p.SettledCounter || !Guid.TryParseExact(p.ActiveTicket,"N",out var active) || active == Guid.Empty))
                throw new ArgumentException("Invalid active match ticket.");
            if (p.SettledCounter != 0 && (!Guid.TryParseExact(p.SettledTicket,"N",out var settled) || settled == Guid.Empty))
                throw new ArgumentException("Invalid settled match ticket.");
        }
        public static PlayerProfile Begin(PlayerProfile p, Guid ticket)
        {
            Validate(p); string id = ticket.ToString("N");
            if (ticket == Guid.Empty || id == p.ActiveTicket || id == p.SettledTicket) throw new ArgumentException("Fresh ticket required.");
            var next = p.Copy(); next.MatchCounter = checked(p.MatchCounter + 1);
            next.ActiveCounter = next.MatchCounter; next.ActiveTicket = id; return next;
        }
        public static PlayerProfile Settle(PlayerProfile p, string ticket, long counter, ProfileOutcome outcome, bool terminal, out ProfileChange change)
        {
            Validate(p); var next = p.Copy();
            if (counter > 0 && ticket == p.SettledTicket && counter == p.SettledCounter) { change = ProfileChange.AlreadySettled; return next; }
            if (counter <= 0 || ticket != p.ActiveTicket || counter != p.ActiveCounter) { change = ProfileChange.WrongMatch; return next; }
            if (!terminal || outcome < ProfileOutcome.Win || outcome > ProfileOutcome.Aborted) { change = ProfileChange.InvalidOutcome; return next; }
            if (outcome == ProfileOutcome.Win) next.Medals = Math.Min(MaxMedals, next.Medals + WinMedals);
            next.SettledTicket = ticket; next.SettledCounter = counter; next.ActiveTicket = ""; next.ActiveCounter = 0;
            change = ProfileChange.Applied; return next;
        }
        public static PlayerProfile Buy(PlayerProfile p, int kind, out ProfileChange change)
        {
            Validate(p); var next = p.Copy(); int price = Price(kind);
            if (price < 0) change = ProfileChange.InvalidTower;
            else if (Owns(p, kind)) change = ProfileChange.AlreadyOwned;
            else if (p.Medals < price) change = ProfileChange.InsufficientMedals;
            else { next.Medals -= price; next.TowerMask |= 1 << kind; change = ProfileChange.Applied; }
            return next;
        }
    }
}
