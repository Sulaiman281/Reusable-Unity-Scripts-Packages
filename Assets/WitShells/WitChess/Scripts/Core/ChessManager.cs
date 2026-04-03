using System;
using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class ChessManager
    {
        public static ChessManager Instance { get; private set; }

        public GameState GameState { get; private set; }
        public Board Board => GameState.Board;
        public EPlayer CurrentPlayer => GameState.CurrentPlayer;
        public Result Result => GameState.Result;
        public bool IsGameOver => GameState.IsGameOver();

        public event Action<EPlayer> OnTurnSwitched;
        public event Action<Result> OnGameOver;
        public event Action<EPlayer, Spot> OnCheck;
        public event Action<Move> OnMoveMade;
        public event Action<Move> OnMoveUndone;

        private readonly Dictionary<Spot, Move> _moveCache = new();
        private Piece _selectedPiece;

        public ChessManager()
        {
            Instance = this;
        }

        public void Setup(Board board, EPlayer startingPlayer = EPlayer.White)
        {
            GameState = new GameState(startingPlayer, board);
            GameState.OnCheck += (player, spot) => OnCheck?.Invoke(player, spot);
        }

        public IEnumerable<Move> LegalMovesForPiece(Spot spot)
            => GameState.LegalMovesForPiece(spot);

        public IEnumerable<Move> AllLegalMovesFor(EPlayer player)
            => GameState.AllLegalMovesFor(player);

        public bool SelectPiece(Spot spot)
        {
            IEnumerable<Move> moves = GameState.LegalMovesForPiece(spot);
            if (!moves.Any()) return false;
            _selectedPiece = GameState.Board[spot];
            CacheMoves(moves);
            return true;
        }

        public IReadOnlyDictionary<Spot, Move> GetCachedMoves() => _moveCache;

        public bool HasCachedMove(Spot to, out Move move)
            => _moveCache.TryGetValue(to, out move);

        public void ClearSelection()
        {
            _selectedPiece = null;
            _moveCache.Clear();
        }

        public void ExecuteMove(Move move)
        {
            GameState.MakeMove(move);
            OnMoveMade?.Invoke(move);

            if (GameState.IsGameOver())
                OnGameOver?.Invoke(GameState.Result);
            else
                OnTurnSwitched?.Invoke(GameState.CurrentPlayer);
        }

        public void ExecutePromotion(Spot from, Spot to, EPieceType newType)
        {
            ExecuteMove(new PawnPromotion(from, to, newType));
        }

        public bool UndoMove()
        {
            if (GameState.MoveCount == 0) return false;
            Move last = GameState.LastMove();
            GameState.UndoMove();
            ClearSelection();
            OnMoveUndone?.Invoke(last);
            OnTurnSwitched?.Invoke(GameState.CurrentPlayer);
            return true;
        }

        public Move ExtractMove(string moveString, EPlayer player)
        {
            foreach (Move move in GameState.AllLegalMovesFor(player))
            {
                if (move.ToString().Equals(moveString, StringComparison.OrdinalIgnoreCase))
                    return move;
            }
            throw new Exception($"Invalid move: {moveString}");
        }

        public void SetGameOver(EEndReason reason, EPlayer winner = EPlayer.None)
        {
            GameState.SetGameOver(reason, winner);
            OnGameOver?.Invoke(GameState.Result);
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            _moveCache.Clear();
            foreach (Move move in moves)
                _moveCache[move.ToPos] = move;
        }
    }
}