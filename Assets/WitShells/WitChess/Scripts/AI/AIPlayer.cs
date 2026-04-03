using System;
using System.Collections.Generic;
using System.Linq;
using WitShells.ThreadingJob;

namespace WitChess
{
    /// <summary>
    /// AI opponent using Negamax with alpha-beta pruning.
    ///
    /// Strategy priority (encoded in evaluation weights):
    ///   1. Checkmate / avoid checkmate
    ///   2. King safety (check penalty, pawn shield, castling rights)
    ///   3. Tactics — defend hanging pieces, capture free opponent pieces
    ///   4. Material exchange — only trade if gain >= 0; never sacrifice
    ///      unless 1-3 moves ahead confirm higher gain or forced checkmate
    ///   5. Opening development — minor pieces off back rank, center pawns
    ///   6. Mobility — more available moves = more space
    /// </summary>
    public class AIPlayer : Player
    {
        // Search depth (ply). 4 = strong enough for casual play.
        public int Depth { get; set; } = 4;

        // Centipawn piece values
        public static readonly Dictionary<EPieceType, int> PieceValues = new()
        {
            { EPieceType.Pawn,   100 },
            { EPieceType.Knight, 300 },
            { EPieceType.Bishop, 300 },
            { EPieceType.Rook,   500 },
            { EPieceType.Queen,  800 },
            { EPieceType.King,   10000 },
        };

        private const int MateScore = 50000;
        private const int DrawScore = 0;

        // Evaluation weights
        private const int ThreatHangingPenalty = 50;  // per 100 of piece value
        private const int ThreatContestedPenalty = 10;
        private const int CheckPenalty = 80;
        private const int CastlingRightsBonus = 20;
        private const int PawnShieldBonus = 10;
        private const int DevelopedMinorBonus = 25;
        private const int CenterPawnBonus = 15;
        private const int CentralSquareBonus = 10;
        private const int MobilityWeight = 3;

        private readonly GameState _gameState;

        public AIPlayer(GameState gameState)
        {
            _gameState = gameState;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public override void NotifyTurnToMove()
        {
            QuickThreadJobs.RunFunction(
                FindBestMove,
                move => { if (move != null) ChoseMove(move); },
                ex => UnityEngine.Debug.LogError($"[AIPlayer] Search failed: {ex.Message}")
            );
        }

        public Move FindBestMove()
        {
            var moves = OrderMoves(_gameState.AllLegalMovesFor(PlayerType), _gameState).ToList();
            if (moves.Count == 0) return null;

            Move bestMove = moves[0];
            int bestScore = int.MinValue + 1;
            int alpha = int.MinValue + 1;
            int beta = int.MaxValue;

            foreach (Move move in moves)
            {
                GameState copy = _gameState.Copy();
                copy.MakeMove(move);
                int score = -Negamax(copy, Depth - 1, -beta, -alpha, PlayerType.Opponent());

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, score);
            }

            return bestMove;
        }

        // ── Search ────────────────────────────────────────────────────────────

        private int Negamax(GameState state, int depth, int alpha, int beta, EPlayer current)
        {
            if (state.IsGameOver())
                return TerminalScore(state, current);

            if (depth == 0)
                return Evaluate(state, current);

            var moves = OrderMoves(state.AllLegalMovesFor(current), state).ToList();
            if (moves.Count == 0)
                return Evaluate(state, current);

            foreach (Move move in moves)
            {
                GameState copy = state.Copy();
                copy.MakeMove(move);
                int score = -Negamax(copy, depth - 1, -beta, -alpha, current.Opponent());

                if (score >= beta) return beta; // beta cutoff
                alpha = Math.Max(alpha, score);
            }

            return alpha;
        }

        private int TerminalScore(GameState state, EPlayer perspective)
        {
            if (state.Result.Winner == EPlayer.None) return DrawScore;
            return state.Result.Winner == perspective ? MateScore : -MateScore;
        }

        // ── Evaluation ────────────────────────────────────────────────────────

        private int Evaluate(GameState state, EPlayer perspective)
        {
            int score = 0;
            score += MaterialScore(state, perspective);
            score += ThreatScore(state, perspective);
            score += KingSafetyScore(state, perspective);
            score += DevelopmentScore(state, perspective);
            score += MobilityScore(state, perspective);
            return score;
        }

        /// Raw material count from perspective's point of view.
        private static int MaterialScore(GameState state, EPlayer perspective)
        {
            int score = 0;
            foreach (Spot spot in state.Board.PiecePositions())
            {
                Piece piece = state.Board[spot];
                int value = PieceValues[piece.Type];
                score += piece.Player == perspective ? value : -value;
            }
            return score;
        }

        /// Rewards free (undefended) opponent pieces; penalizes our hanging pieces.
        /// Encodes:
        ///   - Capture free pieces immediately (high bonus)
        ///   - Defend our hanging pieces (high penalty if left hanging)
        ///   - Slight penalty/bonus for contested pieces
        private static int ThreatScore(GameState state, EPlayer perspective)
        {
            int score = 0;
            EPlayer opponent = perspective.Opponent();

            foreach (Spot spot in state.Board.PiecePositionsFor(perspective))
            {
                int value = PieceValues[state.Board[spot].Type];
                bool attacked = IsAttackedBy(spot, opponent, state);
                bool defended = IsDefendedBy(spot, perspective, state);

                if (attacked && !defended)
                    score -= value * ThreatHangingPenalty / 100;   // hanging — big penalty
                else if (attacked)
                    score -= value * ThreatContestedPenalty / 100; // contested — small penalty
            }

            foreach (Spot spot in state.Board.PiecePositionsFor(opponent))
            {
                int value = PieceValues[state.Board[spot].Type];
                bool attacked = IsAttackedBy(spot, perspective, state);
                bool defended = IsDefendedBy(spot, opponent, state);

                if (attacked && !defended)
                    score += value * ThreatHangingPenalty / 100;   // free piece — big bonus
                else if (attacked)
                    score += value * ThreatContestedPenalty / 100; // contested — small bonus
            }

            return score;
        }

        /// King safety: check detection, pawn shield, castling rights.
        private static int KingSafetyScore(GameState state, EPlayer perspective)
        {
            int score = 0;
            EPlayer opponent = perspective.Opponent();

            if (state.Board.IsInCheck(perspective)) score -= CheckPenalty;
            if (state.Board.IsInCheck(opponent)) score += CheckPenalty;

            // Castling rights are a future safety asset
            if (state.Board.CastleRightKS(perspective) || state.Board.CastleLeftQS(perspective))
                score += CastlingRightsBonus;
            if (state.Board.CastleRightKS(opponent) || state.Board.CastleLeftQS(opponent))
                score -= CastlingRightsBonus;

            // Pawn shield directly in front of king
            Spot kingSpot = state.Board.FindPiece(EPieceType.King, perspective);
            if (kingSpot != null)
                score += PawnShieldScore(kingSpot, perspective, state);

            return score;
        }

        private static int PawnShieldScore(Spot kingSpot, EPlayer player, GameState state)
        {
            int score = 0;
            Direction forward = player == EPlayer.White ? Direction.Down : Direction.Up;
            Direction[] lateral = { Direction.Left, new Direction(0, 0), Direction.Right };

            foreach (Direction d in lateral)
            {
                Spot shield = kingSpot + forward + d;
                if (!Board.InSide(shield)) continue;
                Piece p = state.Board[shield];
                if (p != null && p.Player == player && p.Type == EPieceType.Pawn)
                    score += PawnShieldBonus;
            }
            return score;
        }

        /// Opening development: minor pieces off back rank, center pawn control.
        /// Only active for the first 30 half-moves (15 full moves per side).
        private static int DevelopmentScore(GameState state, EPlayer perspective)
        {
            if (state.MoveCount > 30) return 0;

            int score = 0;
            int backRank = perspective == EPlayer.White ? 0 : 7;

            foreach (Spot spot in state.Board.PiecePositionsFor(perspective))
            {
                Piece piece = state.Board[spot];

                // Reward developed knights and bishops
                if ((piece.Type == EPieceType.Knight || piece.Type == EPieceType.Bishop)
                    && spot.Row != backRank)
                    score += DevelopedMinorBonus;

                // Reward center file pawns (c–f files)
                if (piece.Type == EPieceType.Pawn && spot.Column >= 2 && spot.Column <= 5)
                    score += CenterPawnBonus;

                // Reward occupation of the four central squares (d4 d5 e4 e5)
                bool inCenter = spot.Row >= 3 && spot.Row <= 4
                             && spot.Column >= 3 && spot.Column <= 4;
                if (inCenter && piece.Type != EPieceType.King)
                    score += CentralSquareBonus;
            }

            return score;
        }

        /// Mobility: more available squares = more options and space.
        private static int MobilityScore(GameState state, EPlayer perspective)
        {
            int ours = state.Board.PiecePositionsFor(perspective)
                .Sum(s => state.Board[s].GetPossibleMoves(s, state.Board).Count());
            int theirs = state.Board.PiecePositionsFor(perspective.Opponent())
                .Sum(s => state.Board[s].GetPossibleMoves(s, state.Board).Count());
            return (ours - theirs) * MobilityWeight;
        }

        // ── Move Ordering (improves alpha-beta cutoffs) ────────────────────────

        private static IEnumerable<Move> OrderMoves(IEnumerable<Move> moves, GameState state)
            => moves.OrderByDescending(m => MoveScore(m, state));

        /// MVV-LVA: Most Valuable Victim / Least Valuable Attacker.
        /// Captures bad for the opponent are searched first → better cutoffs.
        private static int MoveScore(Move move, GameState state)
        {
            int score = 0;
            Piece mover = state.Board[move.FromPos];
            Piece target = state.Board[move.ToPos];

            if (target != null)
                score += PieceValues[target.Type] * 10 - PieceValues[mover.Type];

            if (move is PawnPromotion pp)
                score += PieceValues[pp.NewType];

            if (move is Castle)
                score += 50;

            return score;
        }

        // ── Threat Helpers ────────────────────────────────────────────────────

        private static bool IsAttackedBy(Spot target, EPlayer attacker, GameState state)
            => state.Board.PiecePositionsFor(attacker)
                .Any(s => state.Board[s].GetPossibleMoves(s, state.Board)
                    .Any(m => m.ToPos == target));

        private static bool IsDefendedBy(Spot target, EPlayer defender, GameState state)
            => state.Board.PiecePositionsFor(defender)
                .Where(s => s != target)
                .Any(s => state.Board[s].GetPossibleMoves(s, state.Board)
                    .Any(m => m.ToPos == target));
    }
}