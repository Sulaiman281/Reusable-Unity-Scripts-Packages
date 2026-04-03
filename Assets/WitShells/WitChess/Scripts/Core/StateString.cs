using System.Text;

namespace WitChess
{
    public class StateString
    {
        private readonly StringBuilder _builder = new();

        public StateString(EPlayer currentPlayer, Board board, int halfMoveClock, int fullMoveCount)
        {
            AddPiecePlacement(board);
            _builder.Append(' ');
            AddCurrentPlayer(currentPlayer);
            _builder.Append(' ');
            AddCastlingRights(board);
            _builder.Append(' ');
            AddEnPassant(board, currentPlayer);
            _builder.Append(' ');
            AddHalfMoveClock(halfMoveClock);
            _builder.Append(' ');
            AddFullMoveCount(fullMoveCount);

        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public static char PieceChar(Piece piece)
        {
            char c = piece.Type switch
            {
                EPieceType.Pawn => 'p',
                EPieceType.Rook => 'r',
                EPieceType.Knight => 'n',
                EPieceType.Bishop => 'b',
                EPieceType.Queen => 'q',
                EPieceType.King => 'k',
                _ => ' '
            };

            return piece.Player == EPlayer.White ? char.ToUpper(c) : c;
        }

        private void AddRowData(Board board, int row)
        {
            int empty = 0;

            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];
                if (piece == null)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    _builder.Append(empty);
                    empty = 0;
                }

                _builder.Append(PieceChar(piece));
            }

            if (empty > 0)
            {
                _builder.Append(empty);
            }
        }

        private void AddPiecePlacement(Board board)
        {
            for (int row = 7; row >= 0; row--)
            {
                if (row != 7)
                {
                    _builder.Append('/');
                }

                AddRowData(board, row);
            }
        }

        private void AddCurrentPlayer(EPlayer currentPlayer)
        {
            _builder.Append(currentPlayer == EPlayer.White ? 'w' : 'b');
        }

        private void AddCastlingRights(Board board)
        {
            bool castleWKS = board.CastleRightKS(EPlayer.White);
            bool castleWQS = board.CastleLeftQS(EPlayer.White);
            bool castleBKS = board.CastleRightKS(EPlayer.Black);
            bool castleBQS = board.CastleLeftQS(EPlayer.Black);

            if (!(castleWKS || castleWQS || castleBKS || castleBQS))
            {
                _builder.Append('-');
                return;
            }

            if (castleWKS)
            {
                _builder.Append('K');
            }

            if (castleWQS)
            {
                _builder.Append('Q');
            }

            if (castleBKS)
            {
                _builder.Append('k');
            }

            if (castleBQS)
            {
                _builder.Append('q');
            }
        }

        private void AddEnPassant(Board board, EPlayer currentPlayer)
        {
            if (board.CanCaptureEnPassant(currentPlayer))
            {
                _builder.Append('-');
                return;
            }

            Spot enPassant = board.GetPawnSkipPosition(currentPlayer.Opponent());
            if (enPassant == null)
            {
                _builder.Append('-');
                return;
            }
            char file = (char)('a' + enPassant.Column);
            int rank = 8 - enPassant.Row;
            _builder.Append(file);
            _builder.Append(rank);
        }

        private void AddHalfMoveClock(int halfMoveClock)
        {
            _builder.Append(halfMoveClock);
        }

        private void AddFullMoveCount(int fullMoveCount)
        {
            _builder.Append(fullMoveCount);
        }
    }
}