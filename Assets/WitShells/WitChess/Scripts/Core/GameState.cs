using System;
using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class GameState
    {
        public Board Board { get; private set; }
        public EPlayer CurrentPlayer { get; set; }
        public Result Result { get; private set; } = null;

        private int _noCaptureOrPawnMoveCount = 0;
        private int _fullMoveCount = 1;

        private string _stateString;

        private readonly Dictionary<string, int> stateHistory = new();

        private readonly Stack<Move> _moveHistory = new();

        public int MoveCount => _moveHistory.Count;

        public GameState(EPlayer player, Board board)
        {
            CurrentPlayer = player;
            Board = board;
        }

        public Action<EPlayer, Spot> OnCheck;
        public Action<int> MoveSfxType; // 0 normal move // 1 capture // 2 check

        public IEnumerable<Move> LegalMovesForPiece(Spot from)
        {
            if (Board.IsEmpty(from) || Board[from].Player != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            Piece piece = Board[from];
            IEnumerable<Move> moveCandidates = piece.GetPossibleMoves(from, Board);
            return moveCandidates.Where(move => move.IsLegal(Board));
        }

        public void MakeMove(Move move)
        {
            int sfxType = 0;
            Board.SetPawnSkipPosition(CurrentPlayer, null);
            bool isCaptureOrPawn = move.Execute(Board);
            _moveHistory.Push(move);
            if (isCaptureOrPawn)
            {
                _noCaptureOrPawnMoveCount = 0;
                stateHistory.Clear();
                sfxType = Board.IsEmpty(move.ToPos) ? 0 : move.Type == EMoveType.EnPassant ? 1 : 0;
            }
            else
            {
                _noCaptureOrPawnMoveCount++;
            }

            if (CurrentPlayer == EPlayer.Black)
            {
                _fullMoveCount++;
            }

            if (Board.IsInCheck(CurrentPlayer))
            {
                Spot kingSpot = Board.FindPiece(EPieceType.King, CurrentPlayer.Opponent());
                OnCheck?.Invoke(CurrentPlayer, kingSpot);
                sfxType = 2;
            }

            MoveSfxType?.Invoke(sfxType);

            CurrentPlayer = CurrentPlayer.Opponent();
            UpdateStateString();
            CheckForGameOver();
        }

        public IEnumerable<Move> AllLegalMovesFor(EPlayer player)
        {
            IEnumerable<Move> moveCandidates = Board.PiecePositionsFor(player).SelectMany(pos => Board[pos].GetPossibleMoves(pos, Board));
            return moveCandidates.Where(move => move.IsLegal(Board));
        }

        private void CheckForGameOver()
        {
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                if (Board.IsInCheck(CurrentPlayer.Opponent()))
                {
                    Result = Result.Win(CurrentPlayer.Opponent());
                }
                else
                {
                    Result = Result.Draw(EEndReason.Stalemate);
                }
            }
            else if (Board.InsufficientMaterial())
            {
                Result = Result.Draw(EEndReason.InsufficientMaterial);
            }
            else if (FiftyMoveRule())
            {
                Result = Result.Draw(EEndReason.FiftyMoveRule);
            }
            else if (ThreefoldRepetition())
            {
                Result = Result.Draw(EEndReason.ThreefoldRepetition);
            }
        }

        public bool IsGameOver()
        {
            return Result != null;
        }

        private bool FiftyMoveRule()
        {
            return _noCaptureOrPawnMoveCount / 2 == 50;
        }

        private void UpdateStateString()
        {
            _stateString = new StateString(CurrentPlayer, Board, GetHalfMoveClock(), GetFullMoveCount()).ToString();

            if (stateHistory.ContainsKey(_stateString))
            {
                stateHistory[_stateString]++;
            }
            else
            {
                stateHistory[_stateString] = 1;
            }
        }

        private bool ThreefoldRepetition()
        {
            return stateHistory[_stateString] == 3;
        }

        public GameState Copy()
        {
            GameState gameState = new GameState(CurrentPlayer, Board.Copy());
            gameState.SetStates(_stateString, _noCaptureOrPawnMoveCount, _fullMoveCount, stateHistory, _moveHistory);
            return gameState;
        }

        public void SetStates(string state, int noMovementOrCapture, int fullMoveCount, Dictionary<string, int> stateHistory, Stack<Move> moveHistory)
        {
            _stateString = state;
            _noCaptureOrPawnMoveCount = noMovementOrCapture;
            _fullMoveCount = fullMoveCount;
            foreach (var item in stateHistory)
            {
                this.stateHistory.Add(item.Key, item.Value);
            }

            foreach (var move in moveHistory)
            {
                _moveHistory.Push(move);
            }
        }

        public string GetCurrentState()
        {
            return _stateString;
        }

        // last move
        public Move LastMove()
        {
            if (_moveHistory.Count > 0)
            {
                Move lastMOve = _moveHistory.Peek();
                return lastMOve;
            }
            else
            {
                return null;
            }
        }

        public void UndoMove()
        {
            _stateString = new StateString(CurrentPlayer, Board, GetHalfMoveClock(), GetFullMoveCount()).ToString();

            Move move = _moveHistory.Pop();
            Board.UndoMove(move);
            CurrentPlayer = CurrentPlayer.Opponent();

            if (stateHistory.ContainsKey(_stateString))
            {
                stateHistory[_stateString]--;
            }
            else
            {
                stateHistory[_stateString] = 0;
            }
        }

        public int GetHalfMoveClock()
        {
            return _noCaptureOrPawnMoveCount;
        }

        public int GetFullMoveCount()
        {
            return _fullMoveCount;
        }

        public void SetGameOver(EEndReason eEndReason, EPlayer player)
        {
            Result = new Result(player, eEndReason);
        }

        public List<string> GetMoveHistory()
        {
            List<string> moveHistory = new();
            foreach (var move in _moveHistory)
            {
                moveHistory.Add(move.ToString());
            }

            return moveHistory;
        }
    }
}