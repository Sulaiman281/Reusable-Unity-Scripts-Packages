using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class Bishop : Piece
    {
        public override EPieceType Type => EPieceType.Bishop;

        public static readonly Direction[] directions = new Direction[]
        {
            Direction.UpLeft,
            Direction.UpRight,
            Direction.DownLeft,
            Direction.DownRight
        };

        public Bishop(EPlayer player) : base(player)
        {
        }
        public override Piece Copy()
        {
            Bishop bishop = new Bishop(Player);
            bishop.MoveCount = MoveCount;
            return bishop;
        }

        public override IEnumerable<Move> GetPossibleMoves(Spot from, Board board)
        {
            return MovePositionInDirs(from, board, directions).Select(to => new NormalMove(from, to));
        }
    }
}