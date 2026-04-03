using System.Collections.Generic;
using UnityEngine;

namespace WitChess
{
    public class ChessUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _boardParent;
        [SerializeField] private TileUI _tilePrefab;
        [SerializeField] private ChessUISettings _uiSettings;

        [Header("Game Settings")]
        [SerializeField] private EPlayer _humanPlayer = EPlayer.White;
        [SerializeField] private int _aiDepth = 4;

        private readonly TileUI[,] _tileUIs = new TileUI[8, 8];

        private ChessManager _chess;
        private MainPlayer _mainPlayer;
        private AIPlayer _aiPlayer;

        private Spot _selectedSpot;
        private TileUI _lastMoveTileFrom;
        private TileUI _lastMoveTileTo;

        // Preview + queue state (active during AI's turn)
        private Spot _previewSelectedSpot;
        private readonly Dictionary<Spot, Move> _previewMoveCache = new();
        private Move _queuedMove;
        private Spot _queuedFromSpot;
        private Spot _queuedToSpot;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_uiSettings == null) { Debug.LogError("ChessUISettings not assigned."); return; }
            GenerateLayout();
        }

        private void Start()
        {
            StartGame();
        }

        // ── Game Initialization ───────────────────────────────────────────────

        private void StartGame()
        {
            Board board = BoardFactory.CreateStandard();

            _chess = new ChessManager();
            _chess.OnMoveMade += HandleMoveMade;
            _chess.OnMoveUndone += HandleMoveUndone;
            _chess.OnTurnSwitched += HandleTurnSwitched;
            _chess.OnGameOver += HandleGameOver;
            _chess.OnCheck += HandleCheck;

            _chess.Setup(board, EPlayer.White);

            _mainPlayer = new MainPlayer { PlayerType = _humanPlayer };
            _mainPlayer.OnMoveChosen += move => _chess.ExecuteMove(move);

            _aiPlayer = new AIPlayer(_chess.GameState) { PlayerType = _humanPlayer.Opponent(), Depth = _aiDepth };
            _aiPlayer.OnMoveChosen += move => _chess.ExecuteMove(move);

            RefreshAllPieces();
            NotifyCurrentPlayer();
        }

        // ── Board Layout ──────────────────────────────────────────────────────

        private void GenerateLayout()
        {
            float boardZ = _humanPlayer == EPlayer.White ? 180f : 0f;
            float tileZ = boardZ;

            _boardParent.localEulerAngles = new Vector3(0f, 0f, boardZ);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    TileUI tile = Instantiate(_tilePrefab, _boardParent);
                    tile.name = $"Tile_{row}_{col}";
                    _tileUIs[row, col] = tile;

                    // Counter-rotate each tile so its content stays upright
                    tile.transform.localEulerAngles = new Vector3(0f, 0f, tileZ);

                    bool isLight = (row + col) % 2 == 0;
                    tile.SetColor(isLight
                        ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme.NormalTileColor
                        : _uiSettings.CurrentTemplateScheme.BlackColorScheme.NormalTileColor);

                    tile.SetHighlight(false, Color.clear);
                    tile.SetPieceSprite(null);

                    int r = row, c = col;
                    tile.OnTileClicked += _ => OnTileClicked(r, c);
                }
            }
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void OnTileClicked(int row, int col)
        {
            if (_chess.IsGameOver) return;

            Spot clicked = new Spot(row, col);

            if (_chess.CurrentPlayer == _humanPlayer)
            {
                // Human's turn — normal flow; wipe any leftover queue state
                ClearQueueHighlight();
                _queuedMove = null;

                if (_selectedSpot == null)
                    TrySelectPiece(clicked);
                else
                {
                    if (_chess.HasCachedMove(clicked, out Move move))
                    {
                        ClearHighlights();
                        _selectedSpot = null;
                        _mainPlayer.OnMoveChosen.Invoke(move);
                    }
                    else
                    {
                        ClearHighlights();
                        _selectedSpot = null;
                        TrySelectPiece(clicked);
                    }
                }
            }
            else
            {
                // AI's turn — allow preview and queueing one move
                if (_queuedMove != null)
                {
                    // Any click cancels the queued move
                    ClearQueueHighlight();
                    _queuedMove = null;
                    ClearPreviewHighlights();
                    _previewSelectedSpot = null;
                    _previewMoveCache.Clear();
                    TryPreviewSelect(clicked);
                }
                else if (_previewSelectedSpot == null)
                {
                    TryPreviewSelect(clicked);
                }
                else
                {
                    if (_previewMoveCache.TryGetValue(clicked, out Move move))
                    {
                        // Commit the queued move
                        ClearPreviewHighlights();
                        _previewSelectedSpot = null;
                        _queuedMove = move;
                        _queuedFromSpot = move.FromPos;
                        _queuedToSpot = move.ToPos;
                        ShowQueueHighlight();
                    }
                    else
                    {
                        // Clicked outside legal targets — re-select
                        ClearPreviewHighlights();
                        _previewSelectedSpot = null;
                        _previewMoveCache.Clear();
                        TryPreviewSelect(clicked);
                    }
                }
            }
        }

        private void TrySelectPiece(Spot spot)
        {
            if (!_chess.SelectPiece(spot)) return;
            _selectedSpot = spot;
            HighlightSelection(spot, _chess.GetCachedMoves());
        }

        private void TryPreviewSelect(Spot spot)
        {
            if (_chess.Board.IsEmpty(spot)) return;
            if (_chess.Board[spot].Player != _humanPlayer) return;

            _previewMoveCache.Clear();
            foreach (Move m in _chess.AllLegalMovesFor(_humanPlayer))
                if (m.FromPos == spot)
                    _previewMoveCache[m.ToPos] = m;

            if (_previewMoveCache.Count == 0) return;

            _previewSelectedSpot = spot;
            HighlightPreviewSelection(spot, _previewMoveCache);
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleMoveMade(Move move)
        {
            _lastMoveTileFrom?.SetHighlight(false, Color.clear);
            _lastMoveTileTo?.SetHighlight(false, Color.clear);

            foreach (Move m in move.GetNormalMoves())
            {
                Piece movedPiece = _chess.Board[m.ToPos];
                Sprite moving = _tileUIs[m.FromPos.Row, m.FromPos.Column].GetPieceSprite();
                _tileUIs[m.ToPos.Row, m.ToPos.Column].SetPieceSprite(moving);
                _tileUIs[m.FromPos.Row, m.FromPos.Column].SetPieceSprite(null);

                if (m is PawnPromotion pp && movedPiece != null)
                    _tileUIs[m.ToPos.Row, m.ToPos.Column].SetPieceSprite(GetSprite(pp.NewType, movedPiece.Player));
            }

            // En passant: clear the captured pawn's square separately
            if (move is EnPassant ep)
                _tileUIs[ep.GetCapturedPawnPos().Row, ep.GetCapturedPawnPos().Column].SetPieceSprite(null);

            _lastMoveTileFrom = _tileUIs[move.FromPos.Row, move.FromPos.Column];
            _lastMoveTileTo = _tileUIs[move.ToPos.Row, move.ToPos.Column];
            bool fromLight = (move.FromPos.Row + move.FromPos.Column) % 2 == 0;
            bool toLight = (move.ToPos.Row + move.ToPos.Column) % 2 == 0;
            _lastMoveTileFrom.SetHighlight(true, fromLight
                ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme.FromMoveHighlightColor
                : _uiSettings.CurrentTemplateScheme.BlackColorScheme.FromMoveHighlightColor);
            _lastMoveTileTo.SetHighlight(true, toLight
                ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme.ToMoveHighlightColor
                : _uiSettings.CurrentTemplateScheme.BlackColorScheme.ToMoveHighlightColor);
        }

        private void HandleMoveUndone(Move _)
        {
            _lastMoveTileFrom?.SetHighlight(false, Color.clear);
            _lastMoveTileTo?.SetHighlight(false, Color.clear);
            _lastMoveTileFrom = null;
            _lastMoveTileTo = null;
            RefreshAllPieces();
        }

        private void HandleTurnSwitched(EPlayer _) => NotifyCurrentPlayer();

        private void HandleCheck(EPlayer _, Spot kingSpot)
            => _tileUIs[kingSpot.Row, kingSpot.Column].SetHighlight(true, _uiSettings.CurrentTemplateScheme.CheckHighlightColor);

        private void HandleGameOver(Result result)
            => Debug.Log($"Game Over: {result}");

        // ── Turn Management ───────────────────────────────────────────────────

        private void NotifyCurrentPlayer()
        {
            if (_chess.IsGameOver) return;

            if (_chess.CurrentPlayer == _humanPlayer)
            {
                ClearQueueHighlight();

                if (_queuedMove != null)
                {
                    // Validate the queued move is still legal after AI's move
                    Move validated = null;
                    foreach (Move m in _chess.AllLegalMovesFor(_humanPlayer))
                    {
                        if (m.ToString() == _queuedMove.ToString()) { validated = m; break; }
                    }
                    _queuedMove = null;

                    if (validated != null)
                    {
                        _mainPlayer.OnMoveChosen.Invoke(validated);
                        return;
                    }
                }

                _mainPlayer.NotifyTurnToMove();
            }
            else
            {
                // Clear stale preview before AI starts thinking
                ClearPreviewHighlights();
                _previewSelectedSpot = null;
                _previewMoveCache.Clear();
                _aiPlayer.NotifyTurnToMove();
            }
        }

        // ── Highlights ────────────────────────────────────────────────────────

        private void HighlightSelection(Spot from, IReadOnlyDictionary<Spot, Move> moves)
            => ApplyMoveHighlights(from, moves);

        private void HighlightPreviewSelection(Spot from, Dictionary<Spot, Move> moves)
            => ApplyMoveHighlights(from, moves);

        private void ApplyMoveHighlights(Spot from, IEnumerable<KeyValuePair<Spot, Move>> moves)
        {
            bool fromLight = (from.Row + from.Column) % 2 == 0;
            _tileUIs[from.Row, from.Column].SetHighlight(true, fromLight
                ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme.HighlightedTileColor
                : _uiSettings.CurrentTemplateScheme.BlackColorScheme.HighlightedTileColor);

            foreach (var pair in moves)
            {
                Spot to = pair.Key;
                bool capture = !_chess.Board.IsEmpty(to);
                bool toLight = (to.Row + to.Column) % 2 == 0;
                ColorScheme scheme = toLight
                    ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme
                    : _uiSettings.CurrentTemplateScheme.BlackColorScheme;
                _tileUIs[to.Row, to.Column].SetHighlight(true,
                    capture ? scheme.ToMoveHighlightColor : scheme.HighlightedTileColor);
            }
        }

        private void ClearHighlights()
        {
            if (_selectedSpot != null)
                _tileUIs[_selectedSpot.Row, _selectedSpot.Column].SetHighlight(false, Color.clear);
            foreach (var pair in _chess.GetCachedMoves())
                _tileUIs[pair.Key.Row, pair.Key.Column].SetHighlight(false, Color.clear);
            _chess.ClearSelection();
        }

        private void ClearPreviewHighlights()
        {
            if (_previewSelectedSpot != null)
                _tileUIs[_previewSelectedSpot.Row, _previewSelectedSpot.Column].SetHighlight(false, Color.clear);
            foreach (var pair in _previewMoveCache)
                _tileUIs[pair.Key.Row, pair.Key.Column].SetHighlight(false, Color.clear);
        }

        private void ShowQueueHighlight()
        {
            if (_queuedFromSpot == null || _queuedToSpot == null) return;
            bool fromLight = (_queuedFromSpot.Row + _queuedFromSpot.Column) % 2 == 0;
            bool toLight = (_queuedToSpot.Row + _queuedToSpot.Column) % 2 == 0;
            _tileUIs[_queuedFromSpot.Row, _queuedFromSpot.Column].SetHighlight(true, fromLight
                ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme.FromMoveHighlightColor
                : _uiSettings.CurrentTemplateScheme.BlackColorScheme.FromMoveHighlightColor);
            _tileUIs[_queuedToSpot.Row, _queuedToSpot.Column].SetHighlight(true, toLight
                ? _uiSettings.CurrentTemplateScheme.WhiteColorScheme.ToMoveHighlightColor
                : _uiSettings.CurrentTemplateScheme.BlackColorScheme.ToMoveHighlightColor);
        }

        private void ClearQueueHighlight()
        {
            if (_queuedFromSpot != null)
                _tileUIs[_queuedFromSpot.Row, _queuedFromSpot.Column].SetHighlight(false, Color.clear);
            if (_queuedToSpot != null)
                _tileUIs[_queuedToSpot.Row, _queuedToSpot.Column].SetHighlight(false, Color.clear);
            _queuedFromSpot = null;
            _queuedToSpot = null;
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        private void RefreshAllPieces()
        {
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = _chess.Board[row, col];
                    _tileUIs[row, col].SetPieceSprite(piece != null ? GetSprite(piece.Type, piece.Player) : null);
                }
        }

        private Sprite GetSprite(EPieceType type, EPlayer player)
        {
            Skin skin = _uiSettings.CurrentSkin;
            if (skin == null) return null;
            return player == EPlayer.White
                ? type switch
                {
                    EPieceType.Pawn => skin.WhitePawn,
                    EPieceType.Knight => skin.WhiteKnight,
                    EPieceType.Bishop => skin.WhiteBishop,
                    EPieceType.Rook => skin.WhiteRook,
                    EPieceType.Queen => skin.WhiteQueen,
                    EPieceType.King => skin.WhiteKing,
                    _ => null
                }
                : type switch
                {
                    EPieceType.Pawn => skin.BlackPawn,
                    EPieceType.Knight => skin.BlackKnight,
                    EPieceType.Bishop => skin.BlackBishop,
                    EPieceType.Rook => skin.BlackRook,
                    EPieceType.Queen => skin.BlackQueen,
                    EPieceType.King => skin.BlackKing,
                    _ => null
                };
        }
    }
}