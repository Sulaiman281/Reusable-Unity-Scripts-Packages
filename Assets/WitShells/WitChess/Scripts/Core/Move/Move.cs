namespace WitChess
{
    public abstract class Move
    {
        public abstract EMoveType Type { get; }
        public abstract Spot FromPos { get; }
        public abstract Spot ToPos { get; }
        public virtual Piece CapturedPiece { get; set; }
        public abstract Move[] GetNormalMoves();
        public abstract bool Execute(Board board);
        public abstract void Undo(Board board);
        public virtual bool IsLegal(Board board)
        {
            EPlayer player = board[FromPos].Player.Opponent();
            Board boardCopy = board.Copy();
            Execute(boardCopy);
            return !boardCopy.IsInCheck(player);
        }

        public override string ToString()
        {
            return FromPos.ToString() + ToPos.ToString();
        }
    }
}