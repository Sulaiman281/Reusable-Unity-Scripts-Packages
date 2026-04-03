using System;

namespace WitChess
{
    public abstract class Player
    {
        public EPlayer PlayerType { get; set; }

        public Action<Move> OnMoveChosen;

        public abstract void NotifyTurnToMove();

        protected virtual void ChoseMove(Move move)
        {
            OnMoveChosen?.Invoke(move);
        }
    }
}