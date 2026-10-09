using System;

namespace ChuchuGames.GridPuzzle
{
    /// <summary>
    /// A position in a <see cref="Grid{T}"/>. Row 0 is the bottom row; rows grow upward,
    /// matching a side-view building where row = floor.
    /// </summary>
    [Serializable]
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int Row;
        public readonly int Col;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public Cell Offset(int dRow, int dCol) => new Cell(Row + dRow, Col + dCol);

        public bool Equals(Cell other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => (Row * 397) ^ Col;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => $"({Row},{Col})";
    }

    /// <summary>Orthogonal directions. Diagonals are deliberately not supported.</summary>
    [Flags]
    public enum Direction
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        Sides = Left | Right,
        Vertical = Up | Down,
        All = Sides | Vertical,
    }

    public static class DirectionExtensions
    {
        public static readonly Direction[] Singles = { Direction.Left, Direction.Right, Direction.Up, Direction.Down };

        public static (int dRow, int dCol) ToOffset(this Direction single)
        {
            switch (single)
            {
                case Direction.Left: return (0, -1);
                case Direction.Right: return (0, 1);
                case Direction.Up: return (1, 0);
                case Direction.Down: return (-1, 0);
                default: throw new ArgumentException($"{single} is not a single direction", nameof(single));
            }
        }
    }
}
