using System;
using System.Collections.Generic;
using GhostHotel.Model.Content;

namespace GhostHotel.Model
{
    /// <summary>What a keepsake does while equipped (GDD §4.8: "~13 grant small passives").</summary>
    public enum KeepsakePassive
    {
        None,
        ExtraHint,
        ExtraSwap,
        RevealHidden,
        EctoplasmBonus,
        CalmShield,
    }

    public enum StaffAbility
    {
        CancelNoisy,
        RevealAllHidden,
        ExtraSwap,
    }

    public sealed class StaffMember
    {
        public string Id;
        public string Name;
        public string Description;
        public int Cost;
        public StaffAbility Ability;
    }

    /// <summary>Everything staff and equipped keepsakes add to tonight (pure, so it's unit-tested).</summary>
    public sealed class Perks
    {
        public int ExtraHints;
        public int ExtraSwaps;
        /// <summary>How many unrevealed hidden rules to show tonight (int.MaxValue = all).</summary>
        public int RevealHidden;
        public bool CancelNoisy;
        public float EctoplasmMultiplier = 1f;
        public bool CalmShield;

        public static readonly Perks None = new Perks();

        public static readonly StaffMember[] Staff =
        {
            new StaffMember { Id = "mittens", Name = "Mittens", Cost = 150, Ability = StaffAbility.CancelNoisy,
                Description = "The ghost cat. Purrs so loudly nobody notices the noise. Cancels Noisy everywhere." },
            new StaffMember { Id = "orla", Name = "Madame Orla", Cost = 220, Ability = StaffAbility.RevealAllHidden,
                Description = "A medium who reads guests like tea leaves. Reveals every hidden rule." },
            new StaffMember { Id = "pike", Name = "Groundskeeper Pike", Cost = 180, Ability = StaffAbility.ExtraSwap,
                Description = "Knows every back stair. +1 Swap Token at midnight." },
        };

        public const int MaxKeepsakes = 3;

        /// <summary>Active staff slots by story progress (GDD §4.8: 1 slot → 3).</summary>
        public static int StaffSlots(int night) => night >= 26 ? 3 : night >= 18 ? 2 : night >= 11 ? 1 : 0;

        public static StaffMember FindStaff(string id) => Array.Find(Staff, s => s.Id == id);

        public static KeepsakePassive Passive(GuestData g) =>
            g != null && Enum.TryParse(g.keepsakePassive ?? "", out KeepsakePassive p) ? p : KeepsakePassive.None;

        public static string Describe(KeepsakePassive p)
        {
            switch (p)
            {
                case KeepsakePassive.ExtraHint: return "+1 hint each night";
                case KeepsakePassive.ExtraSwap: return "+1 Swap Token at midnight";
                case KeepsakePassive.RevealHidden: return "Reveals one hidden rule each night";
                case KeepsakePassive.EctoplasmBonus: return "+10% Ectoplasm at dawn";
                case KeepsakePassive.CalmShield: return "Bad nights cost half as much Calm";
                default: return "A memento. No effect, just memories.";
            }
        }

        public static Perks Compute(GameProgress p, Func<string, GuestData> guest)
        {
            var perks = new Perks();
            int slots = StaffSlots(p.night);
            for (int i = 0; i < p.activeStaff.Count && i < slots; i++)
            {
                var s = FindStaff(p.activeStaff[i]);
                if (s == null || !p.hiredStaff.Contains(s.Id)) continue;
                switch (s.Ability)
                {
                    case StaffAbility.CancelNoisy: perks.CancelNoisy = true; break;
                    case StaffAbility.RevealAllHidden: perks.RevealHidden = int.MaxValue; break;
                    case StaffAbility.ExtraSwap: perks.ExtraSwaps++; break;
                }
            }
            for (int i = 0; i < p.equippedKeepsakes.Count && i < MaxKeepsakes; i++)
            {
                var gp = p.guests.Find(x => x.id == p.equippedKeepsakes[i]);
                if (gp == null || !gp.movedOn) continue; // only keepsakes you actually own
                switch (Passive(guest(gp.id)))
                {
                    case KeepsakePassive.ExtraHint: perks.ExtraHints++; break;
                    case KeepsakePassive.ExtraSwap: perks.ExtraSwaps++; break;
                    case KeepsakePassive.RevealHidden: if (perks.RevealHidden != int.MaxValue) perks.RevealHidden++; break;
                    case KeepsakePassive.EctoplasmBonus: perks.EctoplasmMultiplier += 0.1f; break;
                    case KeepsakePassive.CalmShield: perks.CalmShield = true; break;
                }
            }
            return perks;
        }

        /// <summary>Applies the night-start perks to a freshly built hotel (reveals, Mittens).</summary>
        public void ApplyTo(HotelModel hotel, GameProgress p)
        {
            foreach (var g in hotel.Guests)
                if (p.revealedHidden.Contains(g.Def.Id)) g.HiddenRevealed = true;
            int reveal = RevealHidden;
            foreach (var g in hotel.Guests)
            {
                if (reveal <= 0) break;
                if (g.Def.Hidden == null || g.HiddenRevealed || g.IsRiddle) continue;
                g.HiddenRevealed = true;
                reveal--;
            }
            if (CancelNoisy) hotel.Modifiers.Add(new RemoveTagEverywhere(Tags.Noisy));
            hotel.RefreshModifiers();
        }
    }

    /// <summary>Removes a tag from every room (Mittens cancels Noisy).</summary>
    public sealed class RemoveTagEverywhere : ITagModifier
    {
        readonly string _tag;
        public RemoveTagEverywhere(string tag) => _tag = tag;

        public IEnumerable<string> AddedTags => new string[0];
        public IEnumerable<string> RemovedTags => new[] { _tag };

        public void Apply(HotelModel hotel)
        {
            foreach (var c in hotel.Rooms.Cells()) hotel.Room(c).Tags.RemoveModifier(_tag);
        }
    }
}
