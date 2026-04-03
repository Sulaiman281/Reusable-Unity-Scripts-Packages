using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class Queen : Piece
    {
        public override EPieceType Type => EPieceType.Queen;

        private readonly Direction[] dirs = new Direction[]
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right,
            Direction.UpLeft,
            Direction.UpRight,
            Direction.DownLeft,
            Direction.DownRight
        };

        public Queen(EPlayer player) : base(player)
        {
        }
        public override Piece Copy()
        {
            Queen queen = new Queen(Player);
            queen.MoveCount = MoveCount;
            return queen;
        }

        public override IEnumerable<Move> GetPossibleMoves(Spot from, Board board)
        {
            return MovePositionInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}