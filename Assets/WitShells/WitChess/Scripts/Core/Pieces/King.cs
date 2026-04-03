using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class King : Piece
    {
        public override EPieceType Type => EPieceType.King;

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

        public King(EPlayer player) : base(player)
        {
        }
        public override Piece Copy()
        {
            King king = new King(Player);
            king.MoveCount = MoveCount;
            return king;
        }

        private bool CanCastKingSide(Spot from, Board board)
        {
            if (HasMoved) return false;

            Spot rookSpot = new Spot(from.Row, 7);
            Spot[] betweenPositions = new Spot[]
            {
                new Spot(from.Row, 5),
                new Spot(from.Row, 6)
            };

            return IsUnmovedRook(rookSpot, board) && AllEmpty(betweenPositions, board);
        }

        private bool CanCastQueenSide(Spot from, Board board)
        {
            if (HasMoved) return false;

            Spot rookSpot = new Spot(from.Row, 0);
            Spot[] betweenPositions = new Spot[]
            {
                new Spot(from.Row, 1),
                new Spot(from.Row, 2),
                new Spot(from.Row, 3)
            };

            return IsUnmovedRook(rookSpot, board) && AllEmpty(betweenPositions, board);
        }

        private IEnumerable<Spot> MovePositions(Spot from, Board board)
        {
            foreach (Direction dir in dirs)
            {
                Spot to = from + dir;
                if (!Board.InSide(to))
                {
                    continue;
                }
                if (board.IsEmpty(to) || board[to].Player != Player)
                {
                    yield return to;

                }
            }
        }

        public override IEnumerable<Move> GetPossibleMoves(Spot from, Board board)
        {
            foreach (Spot to in MovePositions(from, board))
            {
                yield return new NormalMove(from, to);
            }

            if (CanCastKingSide(from, board))
            {
                yield return new Castle(EMoveType.CastleKS, from);
            }

            if (CanCastQueenSide(from, board))
            {
                yield return new Castle(EMoveType.CastleQS, from);
            }
        }

        public override bool CanCaptureOpponentKing(Spot from, Board board)
        {
            return MovePositions(from, board).Any(to => board[to] is King);
        }

        public static bool IsUnmovedRook(Spot spot, Board board)
        {
            if (board.IsEmpty(spot))
            {
                return false;
            }

            Piece piece = board[spot];
            return !piece.HasMoved && piece.Type == EPieceType.Rook;
        }

        public static bool AllEmpty(IEnumerable<Spot> spots, Board board)
        {
            return spots.All(spot => board.IsEmpty(spot));
        }

    }
}