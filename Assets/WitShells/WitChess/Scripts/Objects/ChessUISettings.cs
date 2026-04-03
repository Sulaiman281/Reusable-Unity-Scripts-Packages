using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitChess
{
    [Serializable]
    public class ColorScheme
    {
        public Color NormalTileColor;
        public Color HighlightedTileColor;
        public Color FromMoveHighlightColor;
        public Color ToMoveHighlightColor;
    }

    [Serializable]
    public class TemplateScheme
    {
        public ColorScheme WhiteColorScheme;
        public ColorScheme BlackColorScheme;
        public Color CheckHighlightColor;
    }

    [CreateAssetMenu(fileName = "ChessUISettings", menuName = "WitChess/ChessUISettings")]
    public class ChessUISettings : ScriptableObject
    {
        [Header("Tile UI Colors")]
        public TemplateScheme DefaultTemplateScheme;
        public TemplateScheme CurrentTemplateScheme;

        public List<TemplateScheme> TemplateSchemes;
        [Header("Index")]
        public int CurrentTemplateSchemeIndex;

        [Header("Piece Skins")]
        public Skin DefaultSkin;
        public Skin CurrentSkin;
        public List<Skin> Skins;





        [ContextMenu("Generate Default Template Scheme")]
        public void GenerateDefaultTemplateScheme()
        {
            DefaultTemplateScheme = new TemplateScheme
            {
                WhiteColorScheme = new ColorScheme
                {
                    NormalTileColor = new Color(0.9f, 0.9f, 0.9f),
                    HighlightedTileColor = new Color(1f, 1f, 0.5f),
                    FromMoveHighlightColor = new Color(0.5f, 1f, 0.5f),
                    ToMoveHighlightColor = new Color(1f, 0.5f, 0.5f)
                },
                BlackColorScheme = new ColorScheme
                {
                    NormalTileColor = new Color(0.2f, 0.2f, 0.2f),
                    HighlightedTileColor = new Color(1f, 1f, 0.5f),
                    FromMoveHighlightColor = new Color(0.5f, 1f, 0.5f),
                    ToMoveHighlightColor = new Color(1f, 0.5f, 0.5f)
                },

                CheckHighlightColor = new Color(1f, 0f, 0f)
            };
        }

        [ContextMenu("Set Default Template Scheme")]
        public void SetDefaultTemplateScheme()
        {
            CurrentTemplateScheme = DefaultTemplateScheme;
        }

        [ContextMenu("Set Index Template")]
        public void SetTemplateScheme()
        {
            int index = CurrentTemplateSchemeIndex;
            if (index >= 0 && index < TemplateSchemes.Count)
            {
                CurrentTemplateScheme = TemplateSchemes[index];
            }
        }

        /// <summary>
        /// Populates TemplateSchemes with 3 soothing presets inspired by chess.com.
        /// Call via right-click context menu on this asset.
        /// </summary>
        [ContextMenu("Generate Soothing Template Schemes")]
        public void GenerateSoothingTemplateSchemes()
        {
            TemplateSchemes = new List<TemplateScheme>
            {
                // ── 0. Classic Green (chess.com default) ─────────────────────
                new TemplateScheme
                {
                    WhiteColorScheme = new ColorScheme
                    {
                        NormalTileColor        = new Color(0.933f, 0.933f, 0.824f), // #EEEED2 cream
                        HighlightedTileColor   = new Color(0.965f, 0.953f, 0.518f), // soft yellow
                        FromMoveHighlightColor = new Color(0.729f, 0.792f, 0.212f), // muted yellow-green
                        ToMoveHighlightColor   = new Color(0.729f, 0.792f, 0.212f),
                    },
                    BlackColorScheme = new ColorScheme
                    {
                        NormalTileColor        = new Color(0.463f, 0.588f, 0.337f), // #769656 sage green
                        HighlightedTileColor   = new Color(0.694f, 0.769f, 0.243f), // brighter sage
                        FromMoveHighlightColor = new Color(0.576f, 0.698f, 0.137f),
                        ToMoveHighlightColor   = new Color(0.576f, 0.698f, 0.137f),
                    },
                    CheckHighlightColor = new Color(0.878f, 0.196f, 0.196f),
                },

                // ── 1. Warm Walnut ────────────────────────────────────────────
                new TemplateScheme
                {
                    WhiteColorScheme = new ColorScheme
                    {
                        NormalTileColor        = new Color(0.941f, 0.851f, 0.710f), // #F0D9B5 warm linen
                        HighlightedTileColor   = new Color(0.969f, 0.902f, 0.482f), // warm straw
                        FromMoveHighlightColor = new Color(0.827f, 0.847f, 0.388f), // gold-green
                        ToMoveHighlightColor   = new Color(0.827f, 0.847f, 0.388f),
                    },
                    BlackColorScheme = new ColorScheme
                    {
                        NormalTileColor        = new Color(0.710f, 0.533f, 0.388f), // #B58863 walnut
                        HighlightedTileColor   = new Color(0.804f, 0.647f, 0.310f), // warm amber
                        FromMoveHighlightColor = new Color(0.663f, 0.741f, 0.243f),
                        ToMoveHighlightColor   = new Color(0.663f, 0.741f, 0.243f),
                    },
                    CheckHighlightColor = new Color(0.878f, 0.196f, 0.196f),
                },

                // ── 2. Ocean Blue ─────────────────────────────────────────────
                new TemplateScheme
                {
                    WhiteColorScheme = new ColorScheme
                    {
                        NormalTileColor        = new Color(0.839f, 0.906f, 0.933f), // #D6E7EE soft sky
                        HighlightedTileColor   = new Color(0.863f, 0.929f, 0.647f), // pale lime
                        FromMoveHighlightColor = new Color(0.573f, 0.816f, 0.604f), // sage teal
                        ToMoveHighlightColor   = new Color(0.573f, 0.816f, 0.604f),
                    },
                    BlackColorScheme = new ColorScheme
                    {
                        NormalTileColor        = new Color(0.353f, 0.557f, 0.698f), // #5A8EB2 slate blue
                        HighlightedTileColor   = new Color(0.435f, 0.694f, 0.647f), // muted teal
                        FromMoveHighlightColor = new Color(0.365f, 0.678f, 0.506f),
                        ToMoveHighlightColor   = new Color(0.365f, 0.678f, 0.506f),
                    },
                    CheckHighlightColor = new Color(0.878f, 0.196f, 0.196f),
                },
            };
        }
    }
}