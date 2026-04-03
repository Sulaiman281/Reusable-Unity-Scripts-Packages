namespace WitChess
{
    public class EnPassant : Move
    {
        public override EMoveType Type => EMoveType.EnPassant;

        public override Spot FromPos { get; }

        public override Spot ToPos { get; }

        private readonly Spot capturedPawnPos;

        public EnPassant(Spot fromPos, Spot toPos)
        {
            FromPos = fromPos;
            ToPos = toPos;

            capturedPawnPos = new Spot(fromPos.Row, toPos.Column);
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
            new NormalMove(FromPos, ToPos).Execute(board);
            CapturedPiece = board[capturedPawnPos];
            board[capturedPawnPos] = null;
            return true;
        }

        public override void Undo(Board board)
        {
            new NormalMove(FromPos, ToPos).Undo(board);
            board[capturedPawnPos] = CapturedPiece;
        }

        public Spot GetCapturedPawnPos()
        {
            return capturedPawnPos;
        }

        public override string ToString()
        {
            return FromPos.ToString() + ToPos.ToString() + "e.p.";
        }
    }
}