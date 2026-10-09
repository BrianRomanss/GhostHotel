using System.Collections.Generic;
using ChuchuGames.Core;
using ChuchuGames.GridPuzzle;

namespace GhostHotel.Model
{
    /// <summary>
    /// Place, move, swap or return-to-queue as one undoable step. Undo restores the exact
    /// prior positions of every guest the move touched.
    /// </summary>
    public sealed class PlaceGuestCommand : ICommand
    {
        readonly HotelModel _hotel;
        readonly GuestState _guest;
        readonly Cell? _target;
        Dictionary<GuestState, Cell?> _before;

        public PlaceGuestCommand(HotelModel hotel, GuestState guest, Cell? target)
        {
            _hotel = hotel;
            _guest = guest;
            _target = target;
        }

        public string Name => _target.HasValue ? $"Place {_guest.Def.Name}" : $"Unplace {_guest.Def.Name}";

        public void Execute()
        {
            _before = new Dictionary<GuestState, Cell?> { [_guest] = _guest.Room };
            if (_target.HasValue && _hotel.GuestAt(_target.Value) is GuestState other && other != _guest)
                _before[other] = other.Room;
            _hotel.Place(_guest, _target);
        }

        public void Undo() => _hotel.Restore(_before);
    }

    /// <summary>"Clear all": sends every guest back to the queue, undoably.</summary>
    public sealed class ClearAllCommand : ICommand
    {
        readonly HotelModel _hotel;
        Dictionary<GuestState, Cell?> _before;

        public ClearAllCommand(HotelModel hotel) => _hotel = hotel;

        public string Name => "Clear all";

        public void Execute()
        {
            _before = _hotel.SnapshotPositions();
            var empty = new Dictionary<GuestState, Cell?>();
            foreach (var g in _hotel.Guests)
                if (!g.Locked) empty[g] = null;
            _hotel.Restore(empty);
        }

        public void Undo() => _hotel.Restore(_before);
    }
}
