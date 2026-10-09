using System.Collections.Generic;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using NUnit.Framework;

namespace GhostHotel.Tests
{
    public class GreyboxNightTests
    {
        [Test]
        public void HasExactlyOnePerfectArrangement()
        {
            var h = GreyboxNight.Create();
            var cells = new List<Cell>(h.Rooms.Cells());
            int perfect = 0;
            Permute(h, cells, 0, new bool[cells.Count], ref perfect);
            Assert.AreEqual(1, perfect);
        }

        [Test]
        public void KnownSolution_IsPerfect()
        {
            var h = GreyboxNight.Create();
            // Gloria 101, Morrow 102, Rattles 103, Ashworth 104
            h.Place(h.Guests[0], new Cell(0, 0));
            h.Place(h.Guests[1], new Cell(0, 1));
            h.Place(h.Guests[3], new Cell(0, 2));
            h.Place(h.Guests[2], new Cell(0, 3));
            Assert.IsTrue(Scoring.IsPerfect(h));
        }

        static void Permute(HotelModel h, List<Cell> cells, int guestIndex, bool[] used, ref int perfect)
        {
            if (guestIndex == h.Guests.Count)
            {
                if (Scoring.IsPerfect(h)) perfect++;
                return;
            }
            for (int i = 0; i < cells.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                h.Place(h.Guests[guestIndex], cells[i]);
                Permute(h, cells, guestIndex + 1, used, ref perfect);
                h.Place(h.Guests[guestIndex], null);
                used[i] = false;
            }
        }
    }
}
