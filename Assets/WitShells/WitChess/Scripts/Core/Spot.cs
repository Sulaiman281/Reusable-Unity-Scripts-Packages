using System;
using System.Collections.Generic;

namespace WitChess
{

    public class Spot
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public Spot()
        {
        }

        public Spot(int row, int col)
        {
            Row = row;
            Column = col;
        }

        public void Init(int row, int col)
        {
            Row = row;
            Column = col;
        }

        public EPlayer SquareColor()
        {
            if ((Row + Column) % 2 == 0)
            {
                return EPlayer.White;
            }
            else
            {
                return EPlayer.Black;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public override bool Equals(object obj)
        {
            return obj is Spot spot &&
                   Row == spot.Row &&
                   Column == spot.Column;
        }

        public static bool operator ==(Spot a, Spot b)
        {
            return EqualityComparer<Spot>.Default.Equals(a, b);
        }

        public static bool operator !=(Spot a, Spot b)
        {
            return !(a == b);
        }

        public static Spot operator +(Spot a, Direction b)
        {
            return new Spot { Row = a.Row + b.RawDelta, Column = a.Column + b.ColumnDelta };
        }

        public override string ToString()
        {
            return $"{GetColumnChar()}{Row + 1}";
        }

        public char GetColumnChar()
        {
            return (char)('a' + Column);
        }

        public static Spot FromAlgebraic(string algebraic)
        {
            int row = algebraic[1] - '1';
            int col = algebraic[0] - 'a';
            return new Spot(row, col);
        }

    }

}
