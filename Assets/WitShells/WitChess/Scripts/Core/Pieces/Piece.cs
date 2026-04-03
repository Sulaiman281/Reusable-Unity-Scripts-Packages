using System;
using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    [Serializable]
    public abstract class Piece
    {
        public EPlayer Player { get; }
        public virtual EPieceType Type { get; }

        public int MoveCount { get; set; }

        public bool HasMoved => MoveCount > 0;

        protected Piece(EPlayer player)
        {
            Player = player;
        }

        public abstract Piece Copy();

        public abstract IEnumerable<Move> GetPossibleMoves(Spot from, Board board);

        protected IEnumerable<Spot> MovePositionInDir(Spot from, Board board, Direction dir)
        {
            for (Spot spot = from + dir; Board.InSide(spot); spot += dir)
            {
                if (board.IsEmpty(spot))
                {
                    yield return spot;
                    continue;
                }
                Piece piece = board[spot];
                if (piece.Player != Player)
                {
                    yield return spot;
                }
                yield break;
            }
        }

        protected IEnumerable<Spot> MovePositionInDirs(Spot from, Board board, Direction[] dirs)
        {
            return dirs.SelectMany(dir => MovePositionInDir(from, board, dir));
        }

        public virtual bool CanCaptureOpponentKing(Spot from, Board board)
        {
            return GetPossibleMoves(from, board).Any(move => board[move.ToPos] is King king);
        }

        internal int GetPositionalAdvantage(Spot spot, Board board)
        {

            int row = Player == EPlayer.White ? spot.Row : 7 - spot.Row;
            int col = Player == EPlayer.White ? spot.Column : 7 - spot.Column;

            int[,] positionValues = new int[8, 8]
            {
                {0, 0, 0, 0, 0, 0, 0, 0},
                {50, 50, 50, 50, 50, 50, 50, 50},
                {10, 10, 20, 30, 30, 20, 10, 10},
                {5, 5, 10, 25, 25, 10, 5, 5},
                {0, 0, 0, 20, 20, 0, 0, 0},
                {5, -5, -10, 0, 0, -10, -5, 5},
                {5, 10, 10, -20, -20, 10, 10, 5},
                {0, 0, 0, 0, 0, 0, 0, 0}
            };

            return positionValues[row, col];
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}