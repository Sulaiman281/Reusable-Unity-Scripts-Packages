using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class Rook : Piece
    {
        public override EPieceType Type => EPieceType.Rook;

        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        public Rook(EPlayer player) : base(player)
        {
        }
        public override Piece Copy()
        {
            Rook rook = new Rook(Player);
            rook.MoveCount = MoveCount;
            return rook;
        }

        public override IEnumerable<Move> GetPossibleMoves(Spot from, Board board)
        {
            return MovePositionInDirs(from, board, dirs).Select(to => new NormalMove(from, to)); 
        }
    }
}