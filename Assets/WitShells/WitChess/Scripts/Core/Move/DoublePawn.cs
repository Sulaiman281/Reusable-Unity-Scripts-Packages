namespace WitChess
{
    public class DoublePawn : Move
    {
        public override EMoveType Type => EMoveType.DoublePawn;

        public override Spot FromPos { get; }

        public override Spot ToPos { get; }

        private readonly Spot skipPos;

        public DoublePawn(Spot fromPos, Spot toPos)
        {
            FromPos = fromPos;
            ToPos = toPos;

            skipPos = new Spot((fromPos.Row + toPos.Row) / 2, fromPos.Column);
        }

        public override Move[] GetNormalMoves()
        {
            return new Move[]
            {
                new NormalMove(FromPos, ToPos)
            };
        }

        public override bool Execute(Board board)
        {
            EPlayer player = board[FromPos].Player;
            board.SetPawnSkipPosition(player, skipPos);
            new NormalMove(FromPos, ToPos).Execute(board);
            return true;
        }

        public override void Undo(Board board)
        {
            EPlayer player = board[ToPos].Player;
            board.SetPawnSkipPosition(player, null);
            new NormalMove(FromPos, ToPos).Undo(board);
        }

        public override string ToString()
        {
            return FromPos.ToString() + ToPos.ToString();
        }
    }
}