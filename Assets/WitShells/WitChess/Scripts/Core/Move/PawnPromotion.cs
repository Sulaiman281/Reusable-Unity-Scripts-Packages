namespace WitChess
{
    public class PawnPromotion : Move
    {
        public override EMoveType Type => EMoveType.Promotion;

        public override Spot FromPos { get; }

        public override Spot ToPos { get; }

        private readonly EPieceType _newType;

        public EPieceType NewType => _newType;

        public PawnPromotion(Spot from, Spot to, EPieceType newType)
        {
            FromPos = from;
            ToPos = to;
            _newType = newType;
        }

        private Piece CreatePromotionPiece(EPlayer player)
        {
            return _newType switch
            {
                EPieceType.Queen => new Queen(player),
                EPieceType.Rook => new Rook(player),
                EPieceType.Bishop => new Bishop(player),
                EPieceType.Knight => new Knight(player),
                _ => null
            };
        }

        public override Move[] GetNormalMoves()
        {
            return new Move[]
            {
                new PawnPromotion(FromPos, ToPos, NewType),
            };
        }

        public override bool Execute(Board board)
        {
            Pawn pawn = (Pawn)board[FromPos];
            board[FromPos] = null;

            if (board[ToPos] != null)
            {
                CapturedPiece = board[ToPos];
            }

            Piece promotionPiece = CreatePromotionPiece(pawn.Player);
            promotionPiece.MoveCount = pawn.MoveCount;
            board[ToPos] = promotionPiece;

            return true;
        }

        public override void Undo(Board board)
        {
            Piece promotionPiece = board[ToPos];
            board[ToPos] = CapturedPiece;

            Pawn pawn = new Pawn(promotionPiece.Player);
            pawn.MoveCount = promotionPiece.MoveCount;
            board[FromPos] = pawn;
        }

        public override string ToString()
        {
            return FromPos.ToString() + ToPos.ToString() + GetPromotionChar();
        }

        public char GetPromotionChar()
        {
            return _newType switch
            {
                EPieceType.Queen => 'q',
                EPieceType.Rook => 'r',
                EPieceType.Bishop => 'b',
                EPieceType.Knight => 'n',
                _ => ' '
            };
        }
    }
}