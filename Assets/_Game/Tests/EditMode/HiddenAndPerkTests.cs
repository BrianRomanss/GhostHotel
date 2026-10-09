using System.Linq;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using GhostHotel.Model.Rules;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class HiddenRuleTests
    {
        static (HotelModel h, GuestState g) MirrorShy()
        {
            var f = new ContentFactory(new[]
            {
                new GuestData { id = "vera", name = "Vera", type = "Weeper",
                    likes = new[] { new RuleData { kind = "Edge" } },
                    hiddenRule = new RuleData { kind = "ForbidsTag", tag = "Mirror" },
                    hiddenHint = "I can't bear to see my own face..." },
            });
            var h = f.Night(new NightData { number = 1, floors = 1, roomsPerFloor = 2, guests = new[] { "vera" },
                rooms = new[] { new RoomData { room = "101", tags = new[] { "Mirror" } } } });
            Assert.IsEmpty(f.Errors);
            var g = h.Guests[0];
            h.Place(g, new Cell(0, 0));
            return (h, g);
        }

        [Test]
        public void LiveViewIgnoresIt_DawnTruthCountsIt()
        {
            var (h, g) = MirrorShy();
            Assert.AreEqual(3, Scoring.ScoreGuest(g, h), "the player can't see it yet");
            Assert.AreEqual(2, Scoring.ScoreGuest(g, h, truth: true));
            g.HiddenRevealed = true;
            Assert.AreEqual(2, Scoring.ScoreGuest(g, h), "once known, the live view shows it");
        }

        [Test]
        public void BrokenAtDawn_IsRevealedForFutureStays()
        {
            var (h, _) = MirrorShy();
            var p = new GameProgress();
            var dawn = Economy.ApplyDawn(h, 1, p);
            Assert.AreEqual(2, dawn.Guests[0].Stars);
            Assert.IsTrue(dawn.Guests[0].HiddenRevealed);
            Assert.AreEqual("No Mirror", dawn.Guests[0].HiddenLabel);
            CollectionAssert.Contains(p.revealedHidden, "vera");
        }

        [Test]
        public void SolverRespectsHiddenRules()
        {
            var (h, _) = MirrorShy();
            var r = NightSolver.Analyse(h);
            Assert.AreEqual(102, r.ExamplePerfect["vera"]);
        }

        [Test]
        public void RiddleGuest_NeedsThreeClues()
        {
            var f = new ContentFactory(new[] { new GuestData { id = "m", name = "M", type = "Wisp", riddleClues = new[] { "a", "b" } } });
            f.Night(new NightData { number = 1, floors = 1, roomsPerFloor = 1, guests = new[] { "m" }, riddleGuest = "m" });
            StringAssert.Contains("needs 3 riddleClues", string.Join("\n", f.Errors));
        }
    }

    public class PerkTests
    {
        static GuestData Keeper(string id, string passive) => new GuestData { id = id, name = id, type = "Wisp", keepsakePassive = passive };

        [Test]
        public void StaffSlotsGrowWithTheStory()
        {
            Assert.AreEqual(0, Perks.StaffSlots(10));
            Assert.AreEqual(1, Perks.StaffSlots(11));
            Assert.AreEqual(2, Perks.StaffSlots(18));
            Assert.AreEqual(3, Perks.StaffSlots(30));
        }

        [Test]
        public void ComputeSumsStaffAndOwnedKeepsakes_OnlyWithinSlots()
        {
            var data = new[] { Keeper("finn", "ExtraSwap"), Keeper("pip", "EctoplasmBonus"), Keeper("tobias", "ExtraHint") }
                .ToDictionary(g => g.id);
            var p = new GameProgress { night = 12 };
            p.hiredStaff.AddRange(new[] { "pike", "mittens" });
            p.activeStaff.AddRange(new[] { "pike", "mittens" }); // only 1 slot at night 12
            p.guests.Add(new GuestProgress { id = "finn", movedOn = true });
            p.guests.Add(new GuestProgress { id = "pip", movedOn = true });
            p.guests.Add(new GuestProgress { id = "tobias", movedOn = false }); // not owned yet
            p.equippedKeepsakes.AddRange(new[] { "finn", "pip", "tobias" });

            var perks = Perks.Compute(p, id => data[id]);
            Assert.AreEqual(2, perks.ExtraSwaps, "Pike + Bosun's Whistle");
            Assert.IsFalse(perks.CancelNoisy, "Mittens has no slot yet");
            Assert.AreEqual(1.1f, perks.EctoplasmMultiplier, 1e-4);
            Assert.AreEqual(0, perks.ExtraHints, "an unowned keepsake does nothing");
        }

        [Test]
        public void Mittens_CancelsNoisy()
        {
            var h = new HotelModel(1, 2);
            var p = h.AddGuest(new GuestDef("p", "P", GhostType.Poltergeist, aura: new AuraDef("Noisy", addTag: Tags.Noisy)));
            h.Place(p, new Cell(0, 0));
            Assert.IsTrue(h.HasTag(new Cell(0, 1), Tags.Noisy));
            new Perks { CancelNoisy = true }.ApplyTo(h, new GameProgress());
            Assert.IsFalse(h.HasTag(new Cell(0, 1), Tags.Noisy));
        }

        [Test]
        public void RevealPerks_ShowHiddenRules()
        {
            var h = new HotelModel(1, 3);
            for (int i = 0; i < 3; i++)
            {
                var def = new GuestDef($"g{i}", $"G{i}", GhostType.Wisp) { Hidden = new EdgeRule(), HiddenHint = "?" };
                h.AddGuest(def);
            }
            var p = new GameProgress();
            p.revealedHidden.Add("g2");
            new Perks { RevealHidden = 1 }.ApplyTo(h, p);
            Assert.IsTrue(h.Guests[0].HiddenRevealed, "the Compass reveals the first unknown one");
            Assert.IsFalse(h.Guests[1].HiddenRevealed);
            Assert.IsTrue(h.Guests[2].HiddenRevealed, "already discovered on an earlier stay");
        }

        [Test]
        public void DawnPerks_EctoplasmBonusAndCalmShield()
        {
            var h = new HotelModel(1, 1);
            h.AddGuest(new GuestDef("g", "G", GhostType.Wisp)); // left unplaced: 0 stars, a bad night
            var p = new GameProgress();
            var r = Economy.ApplyDawn(h, 1, p, new Perks { CalmShield = true, EctoplasmMultiplier = 1.1f });
            Assert.AreEqual(-Economy.CalmLoss / 2, r.CalmDelta);

            var good = new HotelModel(1, 1);
            var g = good.AddGuest(new GuestDef("g", "G", GhostType.Wisp));
            good.Place(g, new Cell(0, 0));
            var r2 = Economy.ApplyDawn(good, 2, new GameProgress { night = 2 }, new Perks { EctoplasmMultiplier = 1.1f });
            Assert.AreEqual(22, r2.Ectoplasm); // 2 stars × 10 × 1.1
        }
    }
}
