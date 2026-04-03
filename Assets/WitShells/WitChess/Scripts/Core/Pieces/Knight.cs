using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class Knight : Piece
    {
        public override EPieceType Type => EPieceType.Knight;

        public Knight(EPlayer player) : base(player)
        {
        }
        public override Piece Copy()
        {
            Knight knight = new Knight(Player);
            knight.MoveCount = MoveCount;
            return knight;
        }

        private static IEnumerable<Spot> PotentialToSpots(Spot from)
        {
            foreach (Direction vDir in new Direction[] { Direction.Up, Direction.Down })
            {
                foreach (Direction hDir in new Direction[] { Direction.Left, Direction.Right })
                {
                    yield return from + vDir * 2 + hDir;
                    yield return from + hDir * 2 + vDir;
                }
            }
        }

        private IEnumerable<Spot> MovePositions(Spot from, Board board)
        {
            return PotentialToSpots(from).Where(to => Board.InSide(to)
                && (board.IsEmpty(to) || board[to].Player != Player));
        }   

        public override IEnumerable<Move> GetPossibleMoves(Spot from, Board board)
        {
            return MovePositions(from, board).Select(to => new NormalMove(from, to));
        }
    }
}