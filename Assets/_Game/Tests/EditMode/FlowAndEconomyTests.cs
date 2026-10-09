using System;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class EconomyTests
    {
        static HotelModel SolvedGreybox()
        {
            var h = GreyboxNight.Create();
            h.Place(h.Guests[0], new Cell(0, 0));
            h.Place(h.Guests[1], new Cell(0, 1));
            h.Place(h.Guests[3], new Cell(0, 2));
            h.Place(h.Guests[2], new Cell(0, 3));
            return h;
        }

        [Test]
        public void PerfectFirstPlay_PaysAdvancesAndUnlocksNext()
        {
            var p = new GameProgress();
            var r = Economy.ApplyDawn(SolvedGreybox(), 1, p);

            Assert.IsTrue(r.Perfect);
            Assert.AreEqual(3, r.Moons);
            Assert.AreEqual(4 * 3 * Economy.EctoplasmPerStar + Economy.PerfectNightBonus, r.Ectoplasm);
            Assert.AreEqual(r.Ectoplasm, p.ectoplasm);
            Assert.AreEqual(Economy.StartingCalm + Economy.CalmGain, p.calm);
            Assert.AreEqual(2, p.night);
            Assert.IsTrue(r.Guests.TrueForAll(g => g.StoryAdvanced));
            Assert.AreEqual(1, p.Guest("gloria").staysDone);
            Assert.AreEqual(3, p.Record(1).bestMoons);
        }

        [Test]
        public void Replay_PaysButDoesNotAdvanceStoryOrCalm()
        {
            var p = new GameProgress { night = 5 };
            Economy.ApplyDawn(SolvedGreybox(), 1, p);
            Assert.AreEqual(5, p.night);
            Assert.AreEqual(Economy.StartingCalm, p.calm);
            Assert.AreEqual(0, p.Guest("gloria").staysDone);
            Assert.Greater(p.ectoplasm, 0);
        }

        [Test]
        public void FinalStay_MovesOn_AndGivesAMemory()
        {
            var p = new GameProgress();
            p.Guest("rattles").staysDone = 0;
            p.Guest("morrow").staysDone = 2; // Morrow has 3 stays in GreyboxNight (default)
            var r = Economy.ApplyDawn(SolvedGreybox(), 1, p);
            Assert.IsTrue(p.Guest("morrow").movedOn);
            Assert.AreEqual(1, r.MemoriesGained);
            Assert.AreEqual(1, p.memories);
            Assert.AreEqual(1, p.GuestsMovedOn);
        }

        [Test]
        public void BadNight_CostsCalm_AndHauntquakeAtZero()
        {
            var h = GreyboxNight.Create(); // nobody placed → 0★ average
            var p = new GameProgress { calm = Economy.CalmLoss };
            var r = Economy.ApplyDawn(h, 1, p);
            Assert.AreEqual(-Economy.CalmLoss, r.CalmDelta);
            Assert.IsTrue(r.Hauntquake);
            Assert.AreEqual(Economy.CalmAfterHauntquake, p.calm);
            Assert.AreEqual(1, p.night, "a Hauntquake replays the night");
            Assert.AreEqual(1, r.Moons);
        }

        [Test]
        public void MoonThresholds()
        {
            Assert.AreEqual(1, Economy.Moons(0f));
            Assert.AreEqual(2, Economy.Moons(1.5f));
            Assert.AreEqual(3, Economy.Moons(2.5f));
        }
    }

    public class NightSessionTests
    {
        [Test]
        public void FullLoop_WithoutMidnight()
        {
            var p = new GameProgress();
            var s = new NightSession(1, GreyboxNight.Title, GreyboxNight.Create(), p);
            var phases = "";
            s.PhaseChanged += ph => phases += ph + " ";

            s.BeginCheckIn();
            Assert.IsFalse(s.CanOpenDoors);
            Assert.Throws<InvalidOperationException>(() => s.OpenDoors());

            var h = s.Hotel;
            s.Commands.Execute(new PlaceGuestCommand(h, h.Guests[0], new Cell(0, 0)));
            s.Commands.Execute(new PlaceGuestCommand(h, h.Guests[1], new Cell(0, 1)));
            s.Commands.Execute(new PlaceGuestCommand(h, h.Guests[3], new Cell(0, 2)));
            s.Commands.Execute(new PlaceGuestCommand(h, h.Guests[2], new Cell(0, 3)));
            s.OpenDoors();

            Assert.AreEqual(NightPhase.Dawn, s.Phase);
            Assert.IsTrue(s.Dawn.Perfect);
            Assert.IsFalse(s.Commands.CanUndo, "no undoing after the doors open");
            s.ContinueToDay();
            Assert.AreEqual("CheckIn Dawn Day ", phases);
        }

        [Test]
        public void Midnight_WhenTheNightHasEvents()
        {
            var events = new[] { new MidnightEvent(MidnightKind.SeanceDownstairs, "Séance", "") };
            var s = new NightSession(1, "t", new HotelModel(1, 1), new GameProgress(), events);
            s.BeginCheckIn();
            s.OpenDoors(); // no guests → all placed
            Assert.AreEqual(NightPhase.Midnight, s.Phase);
            Assert.IsNull(s.Dawn);
            s.EndMidnight();
            Assert.IsNotNull(s.Dawn);
        }
    }

    public class RenovationTests
    {
        [Test]
        public void PlaceRemoveRefund_AndAppliedToNight()
        {
            var p = new GameProgress { ectoplasm = 100 };
            var curtains = Renovations.All[0];
            Assert.IsTrue(Renovations.Place(p, curtains, "102"));
            Assert.IsFalse(Renovations.Place(p, curtains, "102"), "same room twice");
            Assert.AreEqual(40, p.ectoplasm);

            var f = new ContentFactory(new GuestData[0]);
            var night = new NightData { number = 1, floors = 1, roomsPerFloor = 2 };
            var h = f.Night(night, p.renovations);
            Assert.IsTrue(h.HasTag(new Cell(0, 1), Tags.Dark));
            night.lockRenovations = true;
            Assert.IsFalse(f.Night(night, p.renovations).HasTag(new Cell(0, 1), Tags.Dark));

            Renovations.Remove(p, p.renovations[0]);
            Assert.AreEqual(100, p.ectoplasm);
        }
    }
}
