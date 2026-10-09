using System;
using System.Collections.Generic;

namespace ChuchuGames.GridPuzzle
{
    /// <summary>A fixed-size rectangular grid of values with orthogonal neighbour queries.</summary>
    public sealed class Grid<T>
    {
        readonly T[] _cells;

        public int Rows { get; }
        public int Cols { get; }
        public int Count => _cells.Length;

        public Grid(int rows, int cols)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));
            Rows = rows;
            Cols = cols;
            _cells = new T[rows * cols];
        }

        public Grid(int rows, int cols, Func<Cell, T> factory) : this(rows, cols)
        {
            foreach (var cell in Cells()) this[cell] = factory(cell);
        }

        public T this[Cell cell]
        {
            get => _cells[IndexOf(cell)];
            set => _cells[IndexOf(cell)] = value;
        }

        public T this[int row, int col]
        {
            get => this[new Cell(row, col)];
            set => this[new Cell(row, col)] = value;
        }

        public bool InBounds(Cell cell) =>
            cell.Row >= 0 && cell.Row < Rows && cell.Col >= 0 && cell.Col < Cols;

        public bool IsTopRow(Cell cell) => cell.Row == Rows - 1;
        public bool IsBottomRow(Cell cell) => cell.Row == 0;
        public bool IsEdgeColumn(Cell cell) => cell.Col == 0 || cell.Col == Cols - 1;

        /// <summary>Linear index, row-major from the bottom-left. Stable for use as a solver variable id.</summary>
        public int IndexOf(Cell cell)
        {
            if (!InBounds(cell)) throw new ArgumentOutOfRangeException(nameof(cell), cell, $"Outside {Rows}x{Cols} grid");
            return cell.Row * Cols + cell.Col;
        }

        public Cell CellAt(int index) => new Cell(index / Cols, index % Cols);

        public IEnumerable<Cell> Cells()
        {
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                yield return new Cell(r, c);
        }

        public IEnumerable<Cell> CellsInRow(int row)
        {
            for (int c = 0; c < Cols; c++) yield return new Cell(row, c);
        }

        /// <summary>
        /// Cells reachable from <paramref name="origin"/> in the given directions, up to
        /// <paramref name="range"/> steps in a straight line. The origin itself is never included.
        /// </summary>
        public IEnumerable<Cell> Neighbours(Cell origin, Direction directions = Direction.All, int range = 1)
        {
            foreach (var single in DirectionExtensions.Singles)
            {
                if ((directions & single) == 0) continue;
                var (dr, dc) = single.ToOffset();
                for (int step = 1; step <= range; step++)
                {
                    var next = origin.Offset(dr * step, dc * step);
                    if (!InBounds(next)) break;
                    yield return next;
                }
            }
        }
    }
}
