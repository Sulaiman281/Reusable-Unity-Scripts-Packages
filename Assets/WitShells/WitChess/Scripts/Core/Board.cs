using System;
using System.Collections.Generic;
using System.Linq;

namespace WitChess
{
    public class Board
    {
        private readonly Piece[,] _pieces = new Piece[8, 8];

        private readonly Dictionary<EPlayer, Spot> skipPawnPositions = new Dictionary<EPlayer, Spot>
        {
            {EPlayer.Black, null},
            {EPlayer.White, null}
        };

        public Piece this[int x, int y]
        {
            get => _pieces[x, y];
            set => _pieces[x, y] = value;
        }

        public Piece this[Spot spot]
        {
            get => _pieces[spot.Row, spot.Column];
            set => _pieces[spot.Row, spot.Column] = value;
        }

        public Spot GetPawnSkipPosition(EPlayer player)
        {
            return skipPawnPositions[player];
        }

        public void SetPawnSkipPosition(EPlayer player, Spot spot)
        {
            skipPawnPositions[player] = spot;
        }

        public Piece CreatePiece(EPieceType pieceType, EPlayer player)
        {
            return pieceType switch
            {
                EPieceType.Pawn => new Pawn(player),
                EPieceType.Rook => new Rook(player),
                EPieceType.Knight => new Knight(player),
                EPieceType.Bishop => new Bishop(player),
                EPieceType.Queen => new Queen(player),
                EPieceType.King => new King(player),
                _ => null
            };
        }

        public static bool InSide(Spot spot)
        {
            return spot.Row >= 0 && spot.Row < 8 && spot.Column >= 0 && spot.Column < 8;
        }

        public bool IsEmpty(Spot spot)
        {
            return InSide(spot) && this[spot] == null;
        }

        public IEnumerable<Spot> PiecePositions()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Spot spot = new Spot(i, j);
                    if (!IsEmpty(spot))
                    {
                        yield return spot;
                    }
                }
            }
        }

        public IEnumerable<Spot> PiecePositionsFor(EPlayer player)
        {
            return PiecePositions().Where(spot => this[spot].Player == player);
        }

        public bool IsInCheck(EPlayer player)
        {
            return PiecePositionsFor(player).Any(spot => this[spot].CanCaptureOpponentKing(spot, this));
        }

        public Board Copy()
        {
            Board board = new Board();
            foreach (Spot spot in PiecePositions())
            {
                board[spot] = this[spot].Copy();
            }

            board.SetSkipPawnPosition(skipPawnPositions);

            return board;
        }

        public void SetSkipPawnPosition(Dictionary<EPlayer, Spot> skipPawnPositions)
        {
            this.skipPawnPositions[EPlayer.White] = skipPawnPositions[EPlayer.White];
            this.skipPawnPositions[EPlayer.Black] = skipPawnPositions[EPlayer.Black];
        }

        public Counting CountingPieces()
        {
            Counting counting = new();
            foreach (Spot spot in PiecePositions())
            {
                counting.Increment(this[spot].Type, this[spot].Player);
            }
            return counting;
        }

        public bool InsufficientMaterial()
        {
            Counting counting = CountingPieces();
            return IsKingVKing(counting) || IsKingBishopVKing(counting) || IsKingKnightVKing(counting)
                    || IsKingBishopVKingBishop(counting);
        }


        private static bool IsKingVKing(Counting counting)
        {
            return counting.TotalCount == 2;
        }

        private static bool IsKingBishopVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(EPieceType.Bishop) == 1 || counting.Black(EPieceType.Bishop) == 1);
        }

        private static bool IsKingKnightVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(EPieceType.Knight) == 1 || counting.Black(EPieceType.Knight) == 1);
        }

        private bool IsKingBishopVKingBishop(Counting counting)
        {
            try
            {
                if (counting.TotalCount != 4) return false;
                if (counting.White(EPieceType.Bishop) != 1 && counting.Black(EPieceType.Bishop) != 1) return false;

                Spot wBishopSpot = FindPiece(EPieceType.Bishop, EPlayer.White);
                Spot bBishopSpot = FindPiece(EPieceType.Bishop, EPlayer.Black);

                return wBishopSpot.SquareColor() == bBishopSpot.SquareColor();
            }
            catch (Exception)
            {
                return false;
            }


        }

        public Spot FindPiece(EPieceType type, EPlayer player)
        {
            return PiecePositionsFor(player).FirstOrDefault(spot => this[spot].Type == type);
        }

        private bool IsUnmovedKingAndRook(Spot kingSpot, Spot rookSpot)
        {
            if (IsEmpty(kingSpot) || IsEmpty(rookSpot)) return false;
            if (this[kingSpot].Type != EPieceType.King || this[rookSpot].Type != EPieceType.Rook) return false;
            return this[kingSpot].MoveCount == 0 && this[rookSpot].MoveCount == 0;
        }

        public bool CastleRightKS(EPlayer player)
        {
            return player switch
            {
                EPlayer.White => IsUnmovedKingAndRook(new Spot(0, 4), new Spot(0, 7)),
                EPlayer.Black => IsUnmovedKingAndRook(new Spot(7, 4), new Spot(7, 7)),
                _ => false
            };
        }

        public bool CastleLeftQS(EPlayer player)
        {
            return player switch
            {
                EPlayer.Black => IsUnmovedKingAndRook(new Spot(7, 4), new Spot(7, 0)),
                EPlayer.White => IsUnmovedKingAndRook(new Spot(0, 4), new Spot(0, 0)),
                _ => false
            };
        }

        private bool HasPawnInPosition(EPlayer player, Spot[] spots, Spot skipPos)
        {
            foreach (Spot spot in spots.Where(InSide))
            {
                Piece piece = this[spot];
                if (piece != null)
                {
                    if (piece.Player != player || piece.Type != EPieceType.Pawn)
                        continue;
                }
                else
                {
                    continue;
                }

                EnPassant enPassant = new EnPassant(spot, skipPos);
                if (enPassant.IsLegal(this))
                {
                    return true;
                }
            }

            return false;

        }

        public bool CanCaptureEnPassant(EPlayer player)
        {
            Spot skipPawnSpot = GetPawnSkipPosition(player);

            if (skipPawnSpot == null) return false;

            Spot[] spots = player switch
            {
                EPlayer.Black => new Spot[] { skipPawnSpot + Direction.DownRight, skipPawnSpot + Direction.DownLeft },
                EPlayer.White => new Spot[] { skipPawnSpot + Direction.UpRight, skipPawnSpot + Direction.UpLeft },
                _ => Array.Empty<Spot>()
            };

            return HasPawnInPosition(player, spots, skipPawnSpot);
        }

        public void UndoMove(Move move)
        {
            move.Undo(this);
        }
    }
}