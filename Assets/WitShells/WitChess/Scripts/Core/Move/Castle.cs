namespace WitChess
{
    public class Castle : Move
    {
        public override EMoveType Type { get; }

        public override Spot FromPos { get; }

        public override Spot ToPos { get; }

        private readonly Direction kingMoveDir;
        public readonly Spot rookFromPos;
        public readonly Spot rookToPos;

        public Castle(EMoveType type, Spot kingPos)
        {
            Type = type;
            FromPos = kingPos;

            if (type == EMoveType.CastleKS)
            {
                kingMoveDir = Direction.Right;
                ToPos = new Spot(kingPos.Row, 6);
                rookFromPos = new Spot(kingPos.Row, 7);
                rookToPos = new Spot(kingPos.Row, 5);
            }
            else if (type == EMoveType.CastleQS)
            {
                kingMoveDir = Direction.Left;
                ToPos = new Spot(kingPos.Row, 2);
                rookFromPos = new Spot(kingPos.Row, 0);
                rookToPos = new Spot(kingPos.Row, 3);
            }
        }

        public override Move[] GetNormalMoves()
        {
            return new Move[]
            {
                new NormalMove(FromPos, ToPos),
                new NormalMove(rookFromPos, rookToPos)
            };
        }

        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board);
            new NormalMove(rookFromPos, rookToPos).Execute(board);
            return false;
        }

        public override bool IsLegal(Board board)
        {
            EPlayer player = board[FromPos].Player.Opponent();

            if (board.IsInCheck(player))
            {
                return false;
            }

            // copy the board
            Board boardCopy = board.Copy();
            Spot kingPosInCopy = FromPos;

            int moves = Type == EMoveType.CastleKS ? 2 : 3;

            for (int i = 0; i < moves; i++)
            {
                new NormalMove(kingPosInCopy, kingPosInCopy + kingMoveDir).Execute(boardCopy);
                kingPosInCopy += kingMoveDir;

                if (boardCopy.IsInCheck(player))
                {
                    return false;
                }
            }

            return true;
        }

        public override void Undo(Board board)
        {
            new NormalMove(FromPos, ToPos).Undo(board);
            new NormalMove(rookFromPos, rookToPos).Undo(board);
        }

        public override string ToString()
        {
            return Type == EMoveType.CastleKS ? "O-O" : "O-O-O";
        }
    }
}