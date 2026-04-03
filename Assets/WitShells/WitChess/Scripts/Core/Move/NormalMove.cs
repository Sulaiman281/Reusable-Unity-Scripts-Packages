namespace WitChess
{
    public class NormalMove : Move
    {
        public override EMoveType Type => EMoveType.Normal;

        public override Spot FromPos { get; }

        public override Spot ToPos { get; }

        public NormalMove(Spot from, Spot to)
        {
            FromPos = from;
            ToPos = to;
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
            Piece piece = board[FromPos];
            bool isCapture = !board.IsEmpty(ToPos);
            if (isCapture)
            {
                CapturedPiece = board[ToPos];
            }
            board[ToPos] = piece;
            board[FromPos] = null;
            piece.MoveCount++;
            return isCapture || piece.Type == EPieceType.Pawn;
        }

        public override void Undo(Board board)
        {
            Piece piece = board[ToPos];
            board[FromPos] = piece;
            board[ToPos] = CapturedPiece;
            piece.MoveCount--;
        }
    }
}