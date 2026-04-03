namespace WitChess
{
    public class Direction
    {
        public readonly static Direction Up = new Direction(-1, 0);
        public readonly static Direction Down = new Direction(1, 0);
        public readonly static Direction Left = new Direction(0, -1);
        public readonly static Direction Right = new Direction(0, 1);
        public readonly static Direction UpLeft = Up + Left;
        public readonly static Direction UpRight = Up + Right;
        public readonly static Direction DownLeft = Down + Left;
        public readonly static Direction DownRight = Down + Right;

        public int RawDelta { get; }
        public int ColumnDelta { get; }

        public Direction(int rawDelta, int columnDelta)
        {
            RawDelta = rawDelta;
            ColumnDelta = columnDelta;
        }

        public static Direction operator +(Direction a, Direction b)
        {
            return new Direction(a.RawDelta + b.RawDelta, a.ColumnDelta + b.ColumnDelta);
        }

        public static Direction operator -(Direction a, Direction b)
        {
            return new Direction(a.RawDelta - b.RawDelta, a.ColumnDelta - b.ColumnDelta);
        }

        public static Direction operator *(Direction a, int b)
        {
            return new Direction(a.RawDelta * b, a.ColumnDelta * b);
        }


    }
}