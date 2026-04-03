using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class Pawn : Piece
    {
        public override EPieceType Type => EPieceType.Pawn;

        private readonly Direction forward;

        public Pawn(EPlayer player) : base(player)
        {
            if (player == EPlayer.White)
            {
                forward = Direction.Down;
            }
            else
            {
                forward = Direction.Up;
            }
        }
        public override Piece Copy()
        {
            Pawn pawn = new Pawn(Player);
            pawn.MoveCount = MoveCount;
            return pawn;
        }

        private static bool CanMoveTo(Spot pos, Board board)
        {
            return Board.InSide(pos) && board.IsEmpty(pos);
        }

        private bool CanCaptureAt(Spot pos, Board board)
        {
            if (!Board.InSide(pos) || board.IsEmpty(pos))
            {
                return false;
            }
            return board[pos].Player != Player;
        }

        private static IEnumerable<Move> PromotionMoves(Spot from, Spot to)
        {
            EPieceType[] promotionTypes = new EPieceType[] { EPieceType.Queen, EPieceType.Rook, EPieceType.Bishop, EPieceType.Knight };
            foreach (EPieceType type in promotionTypes)
            {
                yield return new PawnPromotion(from, to, type);
            }
        }

        private IEnumerable<Move> ForwardMoves(Spot from, Board board)
        {
            Spot to = from + forward;
            if (CanMoveTo(to, board))
            {
                if (to.Row == 0 || to.Row == 7)
                {
                    foreach (Move move in PromotionMoves(from, to))
                    {
                        yield return move;
                    }
                }
                else
                {
                    yield return new NormalMove(from, to);
                    if (!HasMoved)
                    {
                        Spot twoMovePos = to + forward;
                        if (CanMoveTo(twoMovePos, board))
                        {
                            yield return new DoublePawn(from, twoMovePos);
                        }
                    }
                }
            }
        }

        private IEnumerable<Move> DiagonalMoves(Spot from, Board board)
        {
            Direction[] directions = Player == EPlayer.Black ? new Direction[] { Direction.UpLeft, Direction.UpRight } :
             new Direction[] { Direction.DownLeft, Direction.DownRight };
            foreach (Direction dir in directions)
            {
                Spot to = from + dir;

                if (to == board.GetPawnSkipPosition(Player.Opponent()))
                {
                    yield return new EnPassant(from, to);
                }
                else
                {
                    if (CanCaptureAt(to, board))
                    {
                        if (to.Row == 0 || to.Row == 7)
                        {
                            foreach (Move move in PromotionMoves(from, to))
                            {
                                yield return move;
                            }
                        }
                        else
                        {
                            yield return new NormalMove(from, to);
                        }
                    }
                }
            }
        }

        public override IEnumerable<Move> GetPossibleMoves(Spot from, Board board)
        {
            return ForwardMoves(from, board).Concat(DiagonalMoves(from, board));
        }

        public override bool CanCaptureOpponentKing(Spot from, Board board)
        {
            return DiagonalMoves(from, board).Any(move => board[move.ToPos] is King);
        }
    }
}